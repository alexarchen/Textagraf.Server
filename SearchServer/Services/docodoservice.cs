using Docodo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Threading;
using SearchServer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using static Docodo.DBDataSourceBase;

namespace SearchServer
{
    public static class DocodoServiceStatic
    {
        public static DocodoServiceConfigure AddDocodo(this IServiceCollection col,string IndexStorePath=null)
        {
            DocodoService service = new DocodoService(IndexStorePath);
            col.AddSingleton<IDocodoService>(service);
            return new DocodoServiceConfigure(service,col);
        }

        public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> col,int n) 
        {
            Random r = new Random((int)(DateTime.Now.Ticks & 0xFFFFFFF));
            return col.OrderBy(d => r.Next()).Take(n);
        }

    }


    /// <summary>
    /// EntityFramework datasource for DependecyInjection
    /// </summary>
    /// <typeparam name="T">DBContext type</typeparam>
    /// <typeparam name="T">Entity type</typeparam>
    /// 
    /*
    public class EFDataSource<Cont, Ent> where Ent : class
    {
        private Cont _context;
        private IDocodoService _service;
        public EFDataSource(Cont context,IDocodoService service)
        {
            _context = context;
            _service = service;

        }

        private DBSetDataSource<Ent> dBSetDataSource = null;

        public DBSetDataSource<Ent> Build(string name, string basepath, Func<Cont, DbSet<Ent>> entity, string indextype, Func<Ent, bool> select = null)
        {
            if (dBSetDataSource == null)
                dBSetDataSource = new DBSetDataSource<Ent>(name, basepath, entity(_context), indextype, select);

            _service.getBaseIndex().AddDataSource(dBSetDataSource);

            return dBSetDataSource;
        }

    }

    */
    public class DocodoServiceConfigure {
        DocodoService _service;
        IServiceCollection _col;
        public DocodoServiceConfigure(DocodoService service, IServiceCollection col)
        {
            _service = service;
            _col = col;
        }
        /// <summary>
        /// Add datasource to main index
        /// </summary>
        /// <param name="ds">datasource to get data from</param>
        public DocodoServiceConfigure AddDataSource(IIndexDataSource ds) { _service.getBaseIndex().AddDataSource(ds); return this; }

        public DocodoServiceConfigure AddEFDataSource<Cont,Ent>(string name, Func<Cont, IQueryable<Ent>> entity, IndexType indextype, string basepath, string datafieldname) where Ent : class
        {
            //EFDataSource<Cont, Ent>  ef = _col.AddSingleton<EFDataSource<Cont, Ent>>().BuildServiceProvider().GetRequiredService<EFDataSource<Cont, Ent>>();
            Cont cont = _col.BuildServiceProvider().GetRequiredService<Cont>();
            _service.getBaseIndex().AddDataSource(new QueryableDataSource<Ent>(name, basepath, entity(cont), indextype, datafieldname));
//            _service.getBaseIndex().AddDataSource(ef.Build(name,basepath,entity,indextype,select));
            return this;
        }

        /// <summary>
        ///  Ensure main index is created. If not - starts background task to create it.
        /// </summary>
        public DocodoServiceConfigure EnsureCreated()
        {
            Index baseIndex = _service.getBaseIndex();
            if ((!baseIndex.CanSearch) && (baseIndex.CanIndex))
            {
                baseIndex.CreateAsync();
            }
            return this;
        }
    }

    public interface IDocodoService
    {
        Index getIndex(string id);
        string getVersion();
        string[] getLanguages();
        Index createIndex(string id, string[] lang);
        Index getBaseIndex();
    }


    public class DocodoService : IDocodoService, IDisposable
    {
        Dictionary<string, Docodo.Index> indexes = new Dictionary<string, Docodo.Index>();
        Index baseIndex;
        List<Vocab> vocs = new List<Vocab>();
        string Folder { get; set; } = "Base";

        public DocodoService(string folder = null)
        {
            if (folder!=null)
             Folder = folder;
            // load vocabs
            foreach (string file in Directory.GetFiles("Dict\\", "*.voc"))
            {
                vocs.Add(new Vocab(file));
                Console.Write(file.Substring(file.LastIndexOf("\\") + 1).Split('.')[0] + " ");
            }

           // Directory.CreateDirectory("Indexes");
            baseIndex = new Index(Folder,false,vocs.ToArray());
            baseIndex.LoadStopWords("Dict\\stop.txt");
            baseIndex.bKeepForms = true;
            baseIndex.MaxDegreeOfParallelism = 2;
            
        }

        public string getVersion()
        {
            return (indexes.ToString());
        }

        public Index getBaseIndex()
        {
            return baseIndex;
        }

        public Index getIndex(string id)
        {
            if ((id==null) || (id.Length == 0)) return getBaseIndex();
            // TODO: Check lifetime
            if (indexes.ContainsKey(id))
                return indexes[id];
            else
                return new Index();

        }

        public string [] getLanguages()
        {
            List<string> names = new List<string>();
            foreach (Vocab voc in vocs)
                names.Add(voc.Name);

            return names.ToArray();
        }

        public Index createIndex(string id, string [] lang)
        {
            Index ind = new Index("session_indexes\\"+id+"\\");
            ind.LoadStopWords("Dict\\stop.txt");

            foreach (Vocab voc in vocs)
            {
                if (lang.Contains(voc.Name))
                    ind.AddVoc(voc);
            }

            if (indexes.ContainsKey(id))
                indexes[id] = ind;
            else
                indexes.Add(id, ind);
             return ind;

        }


        public void Dispose()
        {
            foreach (Index i in indexes.Values)
                i.Dispose();
            indexes = new Dictionary<string, Index>();
        }
    }

    public class QueryableDataSource<T> : DBDataSourceBase where T : class
    {

        private IQueryable<T> set;
        private Func<T, bool> select;

        public QueryableDataSource(string name, string basepath, IQueryable<T> entities, IndexType indextype, string datafieldname = null, Func<T, bool> select = null) : base(name, basepath, "", "", indextype, datafieldname)
        {
            if (indexType == IndexType.Blob) throw new Exception("Not supported");
            set = entities;
            this.select = select;

        }
        protected override void Navigate(ConcurrentQueue<IIndexDocument> queue, CancellationToken token)
        {
            List<(string name, Type type)> fields =
             typeof(T).GetProperties().Where(p => ((p.GetMethod != null) && (p.PropertyType.IsPublic) && (!p.PropertyType.IsArray) && ((!p.PropertyType.IsClass) || (p.PropertyType.Equals(typeof(string)))))).Select(p => (p.Name, p.PropertyType)).ToList();

            int id = 1;
            try
            {
                Console.WriteLine("Count = " + set.Count());

                //.Where(select == null ? (i) => true : select).AsQueryable().

                set.ForEachAsync<T>((item) =>
                {

                    token.ThrowIfCancellationRequested();

                    StringBuilder builder = new StringBuilder();
                    string primaryname = "";
                    string name = "" + (id++);
                    string fname = "";
                    String text = "";


                    foreach (var field in fields)
                    {
                        string val = item.GetType().GetProperty(field.name).GetGetMethod().Invoke(item, null)?.ToString();

                        if (val != null)
                        {
                            if (field.name.Equals(FieldName)) fname = val;
                            if (field.name.ToLower().Equals("id")) primaryname = val;


                            builder.Append(field.name + "=" + val + "\n");
                        }
                    }
                    if (primaryname.Length > 0) name = primaryname;
                    builder.Append("Name=" + name + "\n");

                    switch (indexType)
                    {
                        case IndexType.File:
                            if (fname.Length > 0)
                                AddRecord(name, fname, builder.ToString(), queue);
                            break;
                        case IndexType.Text:
                            if (text.Length > 0)
                                AddRecord(name, text.ToCharArray(), builder.ToString(), queue);
                            break;

                    }
                }, token).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error creating index: " + e.Message);
            }
        }
    }

}
