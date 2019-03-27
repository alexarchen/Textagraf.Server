using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SearchServer.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Threading;
using System.Xml;
using Microsoft.AspNetCore.Hosting;
using DjvuNet;
using System.Text;
using System.Text.RegularExpressions;
using iTextSharp.text.pdf;

namespace SearchServer
{
    public class ProcessResult
    {
        public int Code;
        public short nPages;
        public DocTitlePageType Title;
    }

    public interface IDocumentProcessor
    {
        void ProcessDocument(string filename,Action<string, ProcessResult, UserContext> onfinish=null);
        //        string GetPageImage(string file,int n);
        //       (int w, int h)[] GetDocStructure(string file);
        string GetDocHtml(string dir);
        byte[] GetThumbnail(string file, int n, int crop=0);

    }


    public class DocumentProcessor : IDocumentProcessor
    {
        //protected class DjvuCache
        //{
        //    public DjvuCache(DjvuDocument doc)
        //    {
        //        this.doc = doc;
        //        used = DateTime.Now;
        //    }
        //    public DjvuCache Update()
        //    {
        //        used = DateTime.Now;
        //        return this;
        //    }

        //    public DjvuDocument doc;
        //    public DateTime used;
        //}


        UserContext _userContext;
        IHostingEnvironment _env;
        // ConcurrentDictionary<string,DjvuCache> cache = new ConcurrentDictionary<string,DjvuCache>();

        public DocumentProcessor(UserContext userContext, IHostingEnvironment env)
        {
            _userContext = userContext;
            _env = env;

            Task.Run(() =>
            {
                while (true)
                {
                    (string file, Action<string, ProcessResult, UserContext> done) val;
                    do
                    {
                        Thread.Sleep(1000);
                    }
                    while (!Queue.TryDequeue(out val));
                    string filename = val.file;
                    Action<string, ProcessResult, UserContext> onfinish = val.done;
                    try
                    {
                        Console.WriteLine("Start processing document: " + filename);
                        string filedir = filename.Substring(0, filename.LastIndexOf("/") + 1);
                        var exitCode = 0;
                        short nPages = 0;
                        DocTitlePageType type = 0;

                        if (filename.ToLower().EndsWith(".pdf"))
                        {
                            {
                                var proc = new Process();
                                proc.StartInfo.FileName = @"mupdf\\mudraw.exe";
                                String fullfile = new FileInfo(filename).FullName;
                                proc.StartInfo.Arguments = $"-o .png -w 800 .pdf 1";
                                proc.StartInfo.WorkingDirectory = filedir; //Directory.GetCurrentDirectory();
                                proc.StartInfo.UseShellExecute = false;
                                proc.StartInfo.RedirectStandardOutput = true;
                                proc.Start();
                                //                 string outPut = proc.StandardOutput.ReadToEnd();
                                proc.WaitForExit(100000);
                                exitCode = proc.ExitCode;
                                if (!proc.HasExited) exitCode = -2;
                                proc.Close();

                                try
                                {
                                    File.Delete(filedir + String.Format(CacheFileName, 1, 1));
                                    File.Delete(filedir + String.Format(CacheFileName, 1, 0));

                                    using (Bitmap bmp = new Bitmap(filedir + ".png"))
                                    {
                                        EncoderParameters myEncoderParameters = new EncoderParameters(1);
                                        EncoderParameter myEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 75L);
                                        myEncoderParameters.Param[0] = myEncoderParameter;
                                        bmp.Save(filedir + String.Format(CacheFileName, 1, 1), GetEncoder(ImageFormat.Jpeg), myEncoderParameters);
                                        type = AnalizePageType(bmp);

                                        IntPtr a = new IntPtr();
                                        bmp.GetThumbnailImage(400, bmp.Height * 400 / bmp.Width, null, a).Save(filename.Substring(0, filename.LastIndexOf("/") + 1) + String.Format(CacheFileName, 1, 0), GetEncoder(ImageFormat.Jpeg), myEncoderParameters);
                                    }

                                    File.Delete(filedir + ".png");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Error thumb: " + e.Message);
                                }
                            }


                            Task.Factory.StartNew(() =>
                            {
                                /*
                                    var proc = new Process();
                                    proc.StartInfo.FileName = "pdf2html\\pdf2htmlEX.exe";
                                    proc.StartInfo.Arguments = "--split-pages 1 --data-dir " + _env.ContentRootPath + "/pdf2html/data --fit-width 740 --embed-outline 1 --process-outline 1 --optimize-text 1 --space-as-offset 1 --correct-text-visibility 1 --embed-external-font 0 --embed-image 0 --embed-font 0 --embed-css 0 --printing 0 \".pdf\"";
                                    proc.StartInfo.WorkingDirectory = filedir; //Directory.GetCurrentDirectory();
                                    proc.StartInfo.UseShellExecute = false;
                                    proc.StartInfo.RedirectStandardOutput = true;
                                    proc.Start();
                                    //                 string outPut = proc.StandardOutput.ReadToEnd();
                                    proc.WaitForExit(); // 3600000 = 60 minutes
                                    exitCode = (exitCode << 8) | proc.ExitCode;
                                    if (!proc.HasExited) exitCode = -2;
                                    proc.Close();
                                    // optimizing images
                                    OptimizeImages(filedir);
                                    // TODO: optimize font, delete unused symbols
                                    //OptimizeFonts(filedir);
                                    EmbedFontsAndImages(filedir);

                                    try
                                    {
                                        // zipping files
                                        GZipFile(filedir + ".html");
                                        foreach (string f in Directory.EnumerateFiles(filedir, "*.page"))
                                        {
                                            GZipFile(f,false);
                                            nPages++;
                                        }

                                    }
                                    catch (Exception e) { }

                                */

                                /*
                                 * VIA mutool and pdf2dvg
                                 */

                                foreach (string file in Directory.EnumerateFiles(filedir, "*.svg"))
                                    File.Delete(file);

                                var proc = new Process();
                                proc.StartInfo.FileName = @"mupdf\\mutool.exe";
                                String fullfile = new FileInfo(filename).FullName;
                                proc.StartInfo.Arguments = $"convert -o .svg .pdf";
                                proc.StartInfo.WorkingDirectory = filedir; //Directory.GetCurrentDirectory();
                                proc.StartInfo.UseShellExecute = false;
                                proc.StartInfo.RedirectStandardOutput = true;
                                proc.Start();
                                //                 string outPut = proc.StandardOutput.ReadToEnd();
                                proc.WaitForExit(100000);
                                exitCode = proc.ExitCode;
                                if (!proc.HasExited) exitCode = -2;
                                proc.Close();

                                /*
                                proc = new Process();
                                proc.StartInfo.FileName = "pdf2html\\pdf2htmlEX.exe";
                                proc.StartInfo.Arguments = "--embed-external-font 1 --embed-font 0 --font-format woff --printing 0 --data-dir " + _env.ContentRootPath + "/pdf2html/data \".pdf\"";
                                proc.StartInfo.WorkingDirectory = filedir; //Directory.GetCurrentDirectory();

                                //                                proc.StartInfo.FileName = @"mupdf\\mutool.exe";
                                //                                proc.StartInfo.Arguments = $"extract .pdf";
                                proc.StartInfo.UseShellExecute = false;
                                proc.StartInfo.RedirectStandardOutput = true;
                                proc.Start();
                                //                 string outPut = proc.StandardOutput.ReadToEnd();
                                proc.WaitForExit(130000);
                                //string res = proc.StandardOutput.ReadToEnd();
                                //var fontTable = GetMuFontTable(proc.StandardOutput);
                                proc.Close();
                                // parsing font table

                                /*
                                foreach (String file in Directory.EnumerateFiles(filedir, "img*.png"))
                                    File.Delete(file);
                                foreach (String file in Directory.EnumerateFiles(filedir, "img*.jpg"))
                                    File.Delete(file);

                                */

                                if (exitCode != -2)
                                {

                                    // convert all fonts
                                    /*
                                    ConvertFonts(Directory.EnumerateFiles(filedir, "*.pfa").ToArray());
                                    ConvertFonts(Directory.EnumerateFiles(filedir, "*.ttf").ToArray());
                                    ConvertFonts(Directory.EnumerateFiles(filedir, "*.cid").ToArray());
                                    ConvertFonts(Directory.EnumerateFiles(filedir, "*.cff").ToArray());
                                    */
                                    /*
                                    proc = new Process();
                                    proc.StartInfo.FileName = @"pdf2svg\\pdf2svg.exe";
                                    proc.StartInfo.Arguments = $"--nothumbs --preserve_fontnames -o fonts .pdf";
                                    proc.StartInfo.WorkingDirectory = filedir; //Directory.GetCurrentDirectory();
                                    proc.StartInfo.UseShellExecute = false;
                                    proc.StartInfo.RedirectStandardOutput = true;
                                    proc.Start();
                                    //                 string outPut = proc.StandardOutput.ReadToEnd();
                                    proc.WaitForExit(100000);
                                    exitCode = proc.ExitCode;
                                    if (!proc.HasExited) exitCode = -2;
                                    proc.Close();

                                    if (exitCode!=-2)
                                    {
                                    */
                                    // combine data

                                    /*
                                    var fontTable = MakeFontTable(filedir); // Directory.EnumerateFiles(filedir, "*.otf").ToDictionary(k => k.Substring(k.LastIndexOf('/')+1, k.LastIndexOf('-')-1-k.LastIndexOf('/')));
                                    nPages = 0;
                                    StringBuilder output = new StringBuilder("<div id='page-container'>");

                                     foreach (string file in Directory.EnumerateFiles(filedir, "*.svg"))
                                      {
                                            nPages++;
                                            Size sz;
                                            InjectSVGFonts(file, filedir, fontTable,out sz);
                                            GZipFile(file,false);

                                        output.Append("<div class='pf'>").Append($"<img width='{sz.Width}' height='{sz.Height}' src='{nPages}.svg'>")
                                         .Append("</div>");
                                      }
                                    output.Append("</div>");

                                    File.WriteAllText(filedir+".html", output.ToString());
                                    //Directory.Delete(filedir + "fonts");
                                    //}

    //                                    foreach (String file in Directory.EnumerateFiles(filedir, "*.woff"))
    //                                       File.Delete(file);
    */

                                    StringBuilder output = new StringBuilder("<div id='page-container'>");
                                    foreach (String file in Directory.EnumerateFiles(filedir, "*.svg"))
                                    {
                                        nPages++;
                                        Size sz;
                                        CompressMuSvg(file,out sz);
                                        output.Append("<div class='pf'>").Append($"<img width='{sz.Width}' height='{sz.Height}' src='{nPages}.svg'>")
                                         .Append("</div>");
                                        GZipFile(file, false);
                                    }
                                    output.Append("</div>");
                                    File.WriteAllText(filedir + ".html", output.ToString());

                                }



                                /*
                                 * 
                                 * VIA DJVU */
                                /*
                               var proc = new Process();
                               proc.StartInfo.FileName = "pdf2djvu/pdf2djvu.exe";
                               //--anti-alias  is very slow
                               proc.StartInfo.Arguments = $"-o {filedir}.djvu -d 600 --bg-subsample 4 {filedir}.pdf";
                               //roc.StartInfo.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(),"pdf2djvu");
                               //proc.StartInfo.UserName = "";
                               proc.StartInfo.UseShellExecute = false;
                               proc.StartInfo.RedirectStandardOutput = true;
                               proc.Start();
                               //                 string outPut = proc.StandardOutput.ReadToEnd();
                               proc.WaitForExit();
                               exitCode = (exitCode << 8) | proc.ExitCode;
                               if (!proc.HasExited) exitCode = -2;
                               // optimizing images
                               proc.Close();

                               DjvuDocument djvuDocument = new DjvuDocument($"{filedir}.djvu");
                               nPages = (short)djvuDocument.Pages.Count;
                               using (StreamWriter s = File.CreateText(filedir + ".html"))
                               {
                                   s.WriteLine("<div id='page-container'>");
                                   for (int q = 0; q < djvuDocument.Pages.Count; q++)
                                   {
                                       s.WriteLine(MakeDjvuPageHtml(djvuDocument, filedir, q + 1));
                                   }
                                   s.WriteLine("</div>");
                               }

                               //File.Delete($"{filedir}.djvu");
                               */
                                onfinish?.Invoke(filename, new ProcessResult() { Title = type, Code = exitCode, nPages = nPages }, _userContext);
                                Console.WriteLine("Finish processing pdf document {0} with code {1}", filename, exitCode);

                                /*
                                                                try
                                                                {
                                                                    // zipping files
                                                                    GZipFile(filedir + ".html");
                                                                    foreach (string f in Directory.EnumerateFiles(filedir, "*.page"))
                                                                    {
                                                                        GZipFile(f);
                                                                        nPages++;
                                                                    }

                                                                }
                                                                catch (Exception e) { }
                                */

                            }).Wait(new TimeSpan(0, 5, 0)); //5 min max, then run next task

                        }
                        else
                        if (filename.ToLower().EndsWith(".epub"))
                        {
                            // unpack epun
                            using (Stream file = File.OpenRead(filename))
                            {
                                using (ZipArchive ar = new ZipArchive(file, ZipArchiveMode.Read))
                                {
                                    ar.ExtractToDirectory(filedir, true);
                                }
                            }

                            // prepare links
                            foreach (string f in Directory.EnumerateFiles(filedir, "*.html"))
                            {
                                string html = File.ReadAllText(f);
                                html = html.Replace("<a ", "<a target='_top' ");
                                File.WriteAllText(f, html);
                            }

                            string opfpath = "content.opf";
                            {
                                XmlDocument xmlDocument = new XmlDocument();
                                xmlDocument.Load(filedir + "/META-INF/container.xml");
                                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
                                nsmgr.AddNamespace("main", "urn:oasis:names:tc:opendocument:xmlns:container");
                                opfpath = xmlDocument.SelectSingleNode("//main:rootfile", nsmgr).Attributes["full-path"].Value;
                            }
                            // now extract image

                            {
                                string Folder = "";
                                if (opfpath.LastIndexOf('/') >= 0)
                                    Folder = opfpath.Substring(0, opfpath.LastIndexOf('/') + 1);

                                XmlDocument xmlDocument = new XmlDocument();
                                xmlDocument.Load(filedir + opfpath);

                                //XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
                                //nsmgr.AddNamespace("", "http://www.idpf.org/2007/opf");

                                XmlNode node = xmlDocument.SelectSingleNode("//*[local-name()='meta'][@name='cover']");
                                if (node != null)
                                {
                                    string id = node.Attributes["content"].Value;
                                    if (id != null)
                                    {
                                        node = xmlDocument.SelectSingleNode("//*[@id='" + id + "']");
                                        if (node != null)
                                        {
                                            string image = node.Attributes["href"].Value;


                                            using (Bitmap bmp = new Bitmap(filedir + Folder + image))
                                            {
                                                IntPtr a = new IntPtr();
                                                EncoderParameters myEncoderParameters = new EncoderParameters(1);
                                                EncoderParameter myEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L);
                                                myEncoderParameters.Param[0] = myEncoderParameter;
                                                bmp.GetThumbnailImage(1000, bmp.Height * 1000 / bmp.Width, null, a).Save(filename.Substring(0, filename.LastIndexOf("/") + 1) + String.Format(CacheFileName, 1, 1), GetEncoder(ImageFormat.Jpeg), myEncoderParameters);
                                                bmp.GetThumbnailImage(400, bmp.Height * 400 / bmp.Width, null, a).Save(filename.Substring(0, filename.LastIndexOf("/") + 1) + String.Format(CacheFileName, 1, 0), GetEncoder(ImageFormat.Jpeg), myEncoderParameters);
                                            }


                                        }
                                    }

                                }
                            }
                            exitCode = 0;

                            onfinish?.Invoke(filename, new ProcessResult() { Title = type, Code = exitCode, nPages = nPages }, _userContext);
                            Console.WriteLine("Finish processing epub document {0} with code {1}", filename, exitCode);

                        }


                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error processing file {filename}: {e.Message}");
                    }
                }
            });

        }

        string ZoomSvgPath(string path,float factor)
        {
            return Regex.Replace(path, @"[-+.\d]+ [-+.\d]+", m => (float.Parse(m.Value.Split(' ')[0]) * factor).ToString("G5")+" "+((-float.Parse(m.Value.Split(' ')[1])) * factor).ToString("G5"));
        }
        
        void AddFontGlyph(string id,string unicode,string path, Dictionary<string,StringBuilder> fonts)
        {
            Match m = Regex.Match(id, @"font_(\w+)_(\w+)");
            if (!m.Success) return;
            string fid = m.Groups[1].Value;
            string gid = m.Groups[2].Value;

            if (!fonts.ContainsKey(fid))
            {
                StringBuilder font = new StringBuilder();
                font.AppendLine("<?xml version='1.0' standalone='no'?>");
                font.AppendLine("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">")
                .AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\">")
                .AppendLine("<defs>")
                .AppendLine("<font id=\"svgtofont\" horiz-adv-x=\"1000\">")
                .AppendLine("<font-face font-family=\"svgtofont\" units-per-em=\"1000\" ascent=\"1000\" descent=\"0\"/>")
                .AppendLine("<missing-glyph horiz-adv-x=\"0\" />");
                fonts[fid] = font;
            }
            if (path!=null)
             fonts[fid].AppendLine($"<glyph glyph-name=\"{gid}\" unicode=\"&#x{unicode};\" ")
                .Append("d=\"").Append(ZoomSvgPath(path,1000)).Append("\" />");
            else
            {
                // finish
                fonts[fid].AppendLine("</font>\n</defs>\n</svg>");
            }
        }


        void CompressMuSvg(string filename,out Size sz)
        {
            sz = new Size();
            try
            {

                Dictionary<string, char> glyphs = new Dictionary<string, char>();
                Dictionary<string, StringBuilder> fonts = new Dictionary<string, StringBuilder>();

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(filename);
                foreach (XmlNode node in xmlDocument.SelectNodes("//*[local-name()='symbol']"))
                {
                    string id = node.Attributes["id"]?.Value;
                    string ch = node.Attributes["ch"]?.Value;
                    string path = node.ChildNodes[0].Attributes["d"]?.Value;
                    if (path != null)
                    {
                        uint chc = uint.Parse(ch, System.Globalization.NumberStyles.HexNumber);
                        if (chc < 32)
                            chc = 0xE000 + chc;
                        if ((chc == 0) || (chc > 0xFFFF))
                        {
                            uint gid = uint.Parse(id.Substring(id.LastIndexOf("_")+1), System.Globalization.NumberStyles.HexNumber);
                            if (gid < 32) gid = 0xE000 + gid;
                            chc = gid;
                        }

                        AddFontGlyph(id, chc.ToString("x"), path, fonts);
                        glyphs[id] = (char)chc;
                        node.ParentNode.RemoveChild(node);
                    }
                }

                var nodes = xmlDocument.SelectNodes("//*[local-name()='g']").OfType<XmlNode>().ToList();
                foreach (XmlNode group in nodes)
                {
                    bool isTextGroup = true;
                    StringBuilder xpos = new StringBuilder();
                    StringBuilder ypos = new StringBuilder();
                    StringBuilder span = new StringBuilder();

                    foreach (XmlNode node in group.ChildNodes)
                    {
                        if (node.Attributes["xlink:href"] != null)
                        {
                            string glyph = node.Attributes["xlink:href"].Value.Substring(1);
                            if (glyphs.ContainsKey(glyph))
                            {
                                string tr = node.Attributes["transform"].Value;
                                tr = tr.Substring(tr.IndexOf("(") + 1);
                                string []xy = tr.Substring(0,tr.LastIndexOf(")")).Split(",");
                                if ((glyphs[glyph] != ' ') || ((span.Length>0) && (span.ToString().Last()!=' ')))
                                {
                                    xpos.Append(Math.Round(float.Parse(xy[0]),2).ToString() + " ");
                                    ypos.Append(Math.Round(float.Parse(xy[1]),2).ToString() + " ");
                                }
                                span.Append(glyphs[glyph]);
                            }
                            else { isTextGroup = false; break; }
                        }
                        else { isTextGroup = false; break; }
                    }

                    if (isTextGroup)
                    {
                        XmlNode text = xmlDocument.CreateElement("text");//CreateNode(XmlNodeType.Element,"text",;
                        text.AppendChild(xmlDocument.CreateElement("tspan"));
                        foreach (XmlAttribute a in group.Attributes.OfType<XmlAttribute>().ToList())
                            text.Attributes.Append(a);
                        text.FirstChild.Attributes.Append(xmlDocument.CreateAttribute("x"));
                        text.FirstChild.Attributes.Append(xmlDocument.CreateAttribute("y"));
                        text.FirstChild.Attributes["x"].Value = xpos.ToString();
                        text.FirstChild.Attributes["y"].Value = ypos.ToString();
                        text.FirstChild.AppendChild(xmlDocument.CreateTextNode(span.ToString()));
                        group.ParentNode.ReplaceChild(text, group);
                    }
                }


                 XmlNode svg = xmlDocument.SelectSingleNode("//*[local-name()='svg']");
                 string[] args = svg.Attributes["viewBox"].Value.Split(' ');
                 sz = new Size((int)Math.Round(float.Parse(args[2])), (int)Math.Round(float.Parse(args[3])));

                 XmlWriterSettings settings = new XmlWriterSettings();
                  settings.Indent = true;
                  settings.IndentChars = "\t";
                  settings.NewLineChars = "\n";
                  settings.NewLineHandling = NewLineHandling.Entitize;

                  string xml = "";
                  using (Stream output = new MemoryStream())
                  {
                    using (XmlWriter wr = XmlWriter.Create(output, settings))
                    {
                        xmlDocument.WriteTo(wr);
                    }
                    output.Position = 0;
                    using (StreamReader reader = new StreamReader(output)) {
                        xml = reader.ReadToEnd();
                    }
                    
                  }
                  // Do some clearing and preparing
                xml = xml.Replace("xmlns=\"\"", "");
                // Save fonts
                string filedir = filename.Substring(0, filename.LastIndexOfAny(new char[] { '/', '\\' }) + 1);
                fonts.Sum(f => {
                    // finish font
                     AddFontGlyph("font_" + f.Key + "_0", "", null, fonts);
                    // write font
                     File.WriteAllText(filedir+"ff_"+f.Key+".svg", f.Value.ToString());
                     return f.Value.Length;
                    });

                // EmbedFonts
                ConvertFonts(Directory.EnumerateFiles(filedir, "ff_*.svg").ToArray(),true);
                // now we have woff files
                // inject them
                StringBuilder css = new StringBuilder("<style>\n<![CDATA[\n");
                css.AppendLine("text {font-size:0.75pt;text-shadow:0 0 0;}");
                foreach (string ff in Directory.EnumerateFiles(filedir, "ff_*.ttf"))
                {
                    string id = ff.Substring(ff.LastIndexOfAny(new char[] { '\\', '/' }) + 1);
                    id = id.Substring(0, id.LastIndexOf('.'));
                    css.Append($".{id}{{font-family:{id};}}\n@font-face{{font-family:{id}; src:url(data:font/ttf;base64,");
                    css.Append(Convert.ToBase64String(File.ReadAllBytes(ff)));
                    css.AppendLine(") format(\"truetype\");}");
                    File.Delete(ff);
                }
                css.AppendLine("]]></style>");
                xml = xml.Insert(xml.IndexOf(">",xml.IndexOf("<svg") + 7)+1, css.ToString());
                File.WriteAllText(filename, xml);

                // DeleteFonts
//                foreach (string file in Directory.EnumerateFiles(filename.Substring(0, filename.LastIndexOfAny(new char[] { '/', '\\' }) + 1), "ff*.svg"))
//                    File.Delete(file);

                
            }
            catch (Exception e) {
                Console.WriteLine("Error: " + e.Message);
            }
            
    }
        /*
            Dictionary <string,string> MakeFontTable(string dir,string format="woff")
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            var reader = new Typography.OpenFont.OpenFontReader();
            Typography.WebFont.WoffDefaultZlibDecompressFunc.DecompressHandler = new Typography.WebFont.ZlibDecompressStreamFunc((input, output) => {
                
                var uncompressed = Ionic.Zlib.ZlibStream.UncompressBuffer(input);
                Array.Copy(uncompressed,output,Math.Min(output.Length, uncompressed.Length));
                return true;
            });
            foreach (string file in Directory.EnumerateFiles(dir, "*." + format))
            {
                using (Stream fs = File.OpenRead(file))
                {
                    var font = reader.Read(fs);
                    if (font!=null)
                     res.TryAdd(font.Name.Replace('+','_'), file);
                }
            }
            return res;
        }
        internal class csselement
        {
            public string css;
            public int used;
            public string font;
        }

        void EmbedFontsAndImages(string dir)
        {
            string csstext = File.ReadAllText(dir + ".css");
            // remove @media blocks
            StringBuilder medias = new StringBuilder();

            //csstext = Regex.Replace(csstext, "@media.+}\n{0,1}}", m => { medias.Append(m.ToString()); return ""; });
            int mediastart = csstext.IndexOf("@media");
            int mediaend = csstext.IndexOf("}\n}", mediastart);
            medias.Append(csstext.Substring(mediastart, mediaend + 3 - mediastart));
            csstext = csstext.Remove(mediastart, mediaend + 3 - mediastart);

            // define css usage
            var css = Regex.Matches(csstext, @"\.(.+){(.+)}").ToDictionary(m => m.Groups[1].ToString(), m =>
            {
                string cssval = m.Groups[0].ToString();
                string font = "";
                if (cssval.Contains("font-family:"))
                {
                    string ff = Regex.Match(cssval, "font-family:(.+);").Groups[1].ToString();
                    font = Regex.Match(csstext, "@font-face{font-family:" + ff + "[^}]+}").ToString();
                }
                return new csselement
                {
                    css = m.Groups[0].ToString(),
                    font  = font,
                    used = 0
                };
            });
            // Embed fonts
            css.Where(kp => kp.Value.font.Length > 0).All(kp=>
             {
                 Regex.Replace(kp.Value.font,@"src:url\((.+)\)",m=>"src:url(data:font/woff;base64,"+ Convert.ToBase64String(File.ReadAllBytes(dir+m.Groups[1].ToString()))+")");
                 return true;
             }
            );

            foreach (string page in Directory.EnumerateFiles(dir, "*.page"))
            {
                string text = File.ReadAllText(page);
                // make used dictionary
                css.All(c => (c.Value.used = 0)==0);
                Regex.Matches(text, "class=\"([^\"]+)\"").Any(m =>
                  m.Groups[1].Value.ToString().Split(' ').Any(v => 
                   (css.ContainsKey(v) && css[v].used++ > 0)));
                // make total css
                StringBuilder pagehtml = new StringBuilder("<html>\n<head>\n<style>\n");
                css.Where(kp => kp.Value.used > 0).Select(kp => $"{kp.Value.font}.{kp.Key}{{{kp.Value}}}").All(s => pagehtml.AppendLine(s)!=null);
                pagehtml.Append(medias);
                pagehtml.Append("</style><body>");
                // Embed images
                Regex.Replace(text, "<img .+src=\"([^\"]+)\"[^>]+>", m =>
                  {
                      string imgfile = m.Groups[1].Value.ToString();
                      if (!File.Exists(dir + imgfile))
                      {
                          if (imgfile.Substring(imgfile.LastIndexOf(".")).Equals(".png", StringComparison.OrdinalIgnoreCase))
                              imgfile = imgfile.Replace(".png", ".jpg", StringComparison.OrdinalIgnoreCase);
                      }
                      string fmt = imgfile.Substring(imgfile.LastIndexOf(".")+1).ToLower();
                      if (fmt.Equals("jpg")) fmt = "jpeg";
                      return m.Value.Replace(imgfile, $"data:image/{fmt};base64,"+ Convert.ToBase64String(File.ReadAllBytes(dir + imgfile)));
                  });
                pagehtml.Append(text).Append("</body></html>");
                File.WriteAllText(page, pagehtml.ToString());
            }
        }

        */
        void ConvertFonts(string [] names, bool del=true)
        {
            if ((names == null) || (names.Length == 0)) return;

            foreach (string file in names)
            {
                string ttf = file.Replace(".svg", ".ttf");
                string ttf1 = file.Replace(".svg", "_1.ttf");
                Process proc = new Process();
                //            proc.StartInfo.FileName = @"fontforge\\fontforge.bat";
                //           proc.StartInfo.Arguments = $"-script fontforge\\convert.pe " + names.Select(n => $"\"{n}\"").Aggregate((a, b) => $"{a} {b}");
                proc.StartInfo.FileName = @"node";
                proc.StartInfo.Arguments = $"svg2ttf\\svg2ttf  \"{file}\" \"{ttf}\"";
                //proc.StartInfo.WorkingDirectory = filedir; //Directory.GetCurrentDirectory();
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                //                 string outPut = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(20000);
                proc.Close();
                /*
                proc = new Process();
                //            proc.StartInfo.FileName = @"fontforge\\fontforge.bat";
                //           proc.StartInfo.Arguments = $"-script fontforge\\convert.pe " + names.Select(n => $"\"{n}\"").Aggregate((a, b) => $"{a} {b}");
                proc.StartInfo.FileName = @"mupdf\\ttfautohint.exe";
                proc.StartInfo.Arguments = $"--symbol -a qqq \"{ttf1}\" \"{ttf}\"";
                //proc.StartInfo.WorkingDirectory = filedir; //Directory.GetCurrentDirectory();
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                //                 string outPut = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(20000);
                proc.Close();*/
                //if (!File.Exists(ttf)) File.Move(ttf1, ttf);
                //else
                //File.Delete(ttf1);
            }

            if (del)
             foreach (string name in names) File.Delete(name);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        // ...
        // Fonts (9):
        //1       (108 0 R):      Type1 'Times-Roman' WinAnsiEncoding(110 0 R)
        //1       (108 0 R):      Type1 'Times-Bold' WinAnsiEncoding(109 0 R)
        //...
        Dictionary<string,string> GetMuFontTable(StreamReader data)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();

            string line = "";
            do
            {
                line = data.ReadLine();
            }
            while ((line != null) && (!line.StartsWith("Fonts (")));
            int nF = 0;
            do
            {
                line = data.ReadLine();
                if ((line!=null) && (line.IndexOf("):") > 0))
                {
                    try
                    {
                        res.Add(Regex.Match(line.Substring(line.IndexOf("):") + 2), @"\w+ '(.+)'").Groups[1].Value, $"_fnt{nF}.otf");
                    }
                    catch(Exception e)
                    {

                    }
                    nF++;
                }
            }
            while (line != null);
            return res;
        }
/*
        void ExtractCMaps(PdfDictionary resource)
        {
            PdfDictionary xobjects = resource.GetAsDict(PdfName.XOBJECT);
            if (xobjects != null)
            {
                foreach (PdfName key in xobjects.Keys)
                {
                    ExtractCMaps(xobjects.GetAsDict(key));
                }
            }
            PdfDictionary fonts = resource.GetAsDict(PdfName.FONT);
            if (fonts == null) return;

            PdfDictionary font;
            foreach (PdfName key in fonts.Keys)
            {
                font = fonts.GetAsDict(key);
                String name = font.GetAsName(PdfName.BASEFONT).ToString();
                if (name.Length > 8 && name.Substring(7, 1) == "+")
                {
                    name = String.Format(
                      "{0} subset ({1})",
                      name.Substring(8), name.Substring(1, 7)
                    );
                }
                else
                {
                    name = name.Substring(1);
                    PdfDictionary desc = font.GetAsDict(PdfName.FONTDESCRIPTOR);
                    if (desc == null)
                    {
                        name += " nofontdescriptor";
                    }
                    else if (desc.Get(PdfName.FONTFILE) != null)
                    {
                        name += " (Type 1) embedded";
                    }
                    else if (desc.Get(PdfName.FONTFILE2) != null)
                    {
                        name += " (TrueType) embedded";
                    }
                    else if (desc.Get(PdfName.FONTFILE3) != null)
                    {
                        name += " (" + font.GetAsName(PdfName.SUBTYPE).ToString().Substring(1) + ") embedded";
                    }
                }
                set.Add(name);
            }
        }


        protected void ExtractCMaps(string filename)
        {

            using (PdfReader reader = new PdfReader(filename))
            {
                PdfDictionary resources;
                for (int k = 1; k <= reader.NumberOfPages; ++k)
                {
                    resources = reader.GetPageN(k).GetAsDict(PdfName.RESOURCES);

                    ExtractCMaps(resources);

                }

            }


        }
*/

        protected void InjectSVGFonts(string filename, string fontsdir, Dictionary<string,string> fontTable, out Size sz)
        {
            string[] weights = { "thin", "ultralight", "light", "semi", "normal", "romain", "bold", "extra" };
            string[] styles = { "italic" };
            sz = new Size();
            try
            {

                HashSet<string> fontNames = new HashSet<string>();

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(filename);

                {
                    XmlNode svgtag = xmlDocument.SelectSingleNode("//*[local-name()='svg']");
                    sz.Width = (int) float.Parse(svgtag.Attributes["viewBox"].Value.Split(" ")[2]);
                    sz.Height = (int) float.Parse(svgtag.Attributes["viewBox"].Value.Split(" ")[3]);
                }

                foreach (XmlNode node in xmlDocument.SelectNodes("//*[local-name()='text']"))
                {
                    string font = node.Attributes["font-family"]?.Value;
                    if (font != null)
                    {
                        font = font.Replace("+", "_");
                        node.Attributes["font-family"].Value = font;
                    }
                    string fontweight = node.Attributes["font-weight"]?.Value.ToLower().Replace("-", "");
                    string fontstyle = node.Attributes["font-style"]?.Value;
                    if (font == null) continue;
                    if (fontweight == null) fontweight = "normal";
                    if (fontstyle == null) fontstyle = "normal";
                    /*
                    if (fontTable.ContainsKey(font.ToLower()))
                    {
                        fontNames.Add(font);
                    }
                    else
                    if (fontTable.ContainsKey(font.ToLower()+"-"+fontweight))
                    {
                        fontNames.Add(font + "-" + fontweight);
                    }
                    else
                    if (fontTable.ContainsKey(font.ToLower() + "-" + fontweight+fontstyle))
                    {
                        fontNames.Add(font + "-" + fontweight + fontstyle);
                    }
                    else
                    if (fontTable.ContainsKey(font.ToLower() + "-" + fontstyle))
                    {
                        fontNames.Add(font + "-" + fontstyle);
                    }
                    else
                    {
                        if (fontstyle.Equals("normal"))
                        {
                            int i = weights.Select((w,index)=> new {w, index }).Where(o=>o.w.Equals(fontweight)).Select(o => o.index).FirstOrDefault();
                            string key = fontTable.Keys.Where(k =>(k.IndexOf('-')>0) && (k.Split('-')[0].Equals(font)) && (weights.Contains(k.ToLower().Split('-')[1])))
                                .OrderBy(k=>Math.Abs(weights.Select((w,index)=>new { w, index }).Where(o => o.w.Equals(k.Split('-')[1].ToLower())).Select(o => o.index).FirstOrDefault()-i)).FirstOrDefault();
                            if (key != null)
                            {
                                fontNames.Add(key);
                                continue;
                            }
                        }

                        Console.WriteLine("Warning! No such font: " + font);
                    }*/
                    if (font!=null)
                     fontNames.Add(font);
                }

               /* using (Stream output = File.Create(filename))
                {
                    XmlWriterSettings settings = new XmlWriterSettings();
                    
                    using (XmlWriter wr = XmlWriter.Create(output))
                    {
                        xmlDocument.WriteTo(wr);

                    }
                }
                */

                String text = File.ReadAllText(filename);
                text =Regex.Replace(text, @" font-family=""(.{ 6})\+(.+)""", " font-family=\"$1_$2\"");
                //HashSet<string> fontNames = Regex.Matches(text, "font-family=\"(.+)\"").Select(m => m.Groups[1].Value).ToHashSet();
                int svg = text.IndexOf("<svg");
                svg = text.IndexOf(">", svg)+1;
                StringBuilder css = new StringBuilder("\n<style type=\"text/css\">\n")
                    .Append("<![CDATA[\n");

                /*
                 * 
                thin - тонкий,
                ultra-light - ультра легкий,
                extra-light - сверх легкий,
                light - легкий,
                semi (semi-light) - полулегкий,
                book - ровный,
                normal - нормальный,
                regular - стандартный,
                roman - прямой,
                plain - прямой,
                medium - средний,
                demi (semi-bold) - полужирный,
                bold - жирный,
                extra (extra-bold) - более жирный,
                heavy - жирный насыщенный,
                black - очень жирный,
                extra-black - сверхжирный,
                ultra (ultra-black) - ультражирный.
                 */

                foreach (string font in fontNames)
                {
                    string style = "normal";
                    string weight = "normal";
                    /*
                    if (font.Split('-').Length>1)
                    {
                        string add = font.Split('-')[1].ToLower();
                        string w = weights.FirstOrDefault(o => add.Contains(o));
                        if (w != null) weight = w;
                        w = styles.FirstOrDefault(o => add.Contains(o));
                        if (w != null) style = w;
                    }*/
                    if ((fontTable.ContainsKey(font)) || ((font[6]=='_') && (fontTable.ContainsKey(font.Substring(7)))))
                    {
                        css.Append($"@font-face {{ font-style:{style}; font-weight:{weight}; font-family:{font}; src:url(data:font/otf;base64,");
                        string key = font;
                        if (!fontTable.ContainsKey(font)) key = font.Substring(7);
                        css.Append(Convert.ToBase64String(File.ReadAllBytes(fontTable[key])));
                       
                        css.Append(") format(\"woff\");}\n\n");
                    }
                }
                css.Append("]]>\n</style>\n");
                text = text.Insert(svg,css.ToString());
                File.WriteAllText(filename, text);
            }
            catch(Exception e)
            {

            }
        }

        /// <summary>
        /// Optimize .png images in folder, convert them to jpg if needed
        /// </summary>
        /// <param name="dir"></param>
        protected void OptimizeImages(string dir)
        {
            foreach (string f in Directory.EnumerateFiles(dir, "*.png"))
            {
                long fs = new FileInfo(f).Length;
                using (Bitmap bmp = new Bitmap(f))
                {
                    float ratio = (bmp.Width * bmp.Height * 3) / fs;
                    if (ratio<20)
                     {
                        // trying to save as jpeg

                        EncoderParameters myEncoderParameters = new EncoderParameters(1);
                        EncoderParameter myEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L);
                        myEncoderParameters.Param[0] = myEncoderParameter;
                        using (MemoryStream str = new MemoryStream())
                        {
                            bmp.Save(str, GetEncoder(ImageFormat.Jpeg), myEncoderParameters);
                            if (str.Length < fs*0.9)
                            {
                                bmp.Dispose();

                                using (Stream stream = File.Create(f))
                                {
                                    str.WriteTo(stream);
                                }
                            }
                        }
                    }
                }
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public int MAX_CACHE_ITEMS = 30;

//        DjvuDocument GetDjvuDocument(string file)
      //  {
            /*
            if (cache.ContainsKey(file))
            {
                return cache[file].Update().doc;
            }
            else {

                if (cache.Count>MAX_CACHE_ITEMS)
                {
                    DjvuCache cc;
                    cache.Remove(cache.OrderBy(c => c.Value.used).Last().Key,out cc);
                    cc.doc.Dispose();
                }
                DjvuDocument doc = new DjvuDocument(file);
                cache.TryAdd(file, new DjvuCache(doc));

                return doc;
            }*/
  //          return (new DjvuDocument(file));

    //    }


        //public (int w,int h)[] GetDocStructure(string file)
        //{
        //    List<(int w, int h)> list = new List<(int w, int h)>();
        //    using (DjvuDocument doc = GetDjvuDocument(file + "/.djvu"))
        //    {
        //        foreach (DjvuPage page in doc.Pages)
        //        {
        //            list.Add((page.Info.Width, page.Info.Height));
        //        }
        //    }
            
        //    return list.ToArray();
        //}
        const string CacheFileName = "/_cache_{0}_{1}.jpg";
        // crop = 0 standard, crop = 1 - large
        public byte[] GetThumbnail(string file,int n,int crop=0)
        {
            string cacheFileName = file + String.Format(CacheFileName, n, crop);
            if (File.Exists(cacheFileName))
                return File.ReadAllBytes(cacheFileName);

            return null;
/*
            using (DjvuDocument doc = GetDjvuDocument(file + "/.djvu"))
            {
                if ((n < 1) || (n > doc.Pages.Count)) return null;

                string cacheFileName = file + String.Format(CacheFileName, n, crop);
                if (File.Exists(cacheFileName))
                    return File.ReadAllBytes(cacheFileName);

                int W = 402; if (crop == 1) W = 804;
                int H = W * doc.Pages[n - 1].Info.Height / doc.Pages[n - 1].Info.Width;
                using (Bitmap thumb = doc.Pages[n - 1].Image.ResizeImage(W, H))
                {
                    using (MemoryStream memory = new MemoryStream())
                    {
                        EncoderParameters myEncoderParameters = new EncoderParameters(1);
                        EncoderParameter myEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 75L);
                        myEncoderParameters.Param[0] = myEncoderParameter;
                        thumb.Save(memory, GetEncoder(ImageFormat.Jpeg), myEncoderParameters);
                        byte[] bytes = memory.ToArray();
                        if (n == 1)
                        {
                            // save cache
                            File.WriteAllBytes(cacheFileName, bytes);
                        }

                        return bytes;

                    }

                }


            }
            */
        }



        // file - must be folder with .djvu file
        // n - page


        struct HSV
        {
            public double H;
            public double S;
            public double V;
        }

        static Color HSVToRGB(HSV hsv)
        {
            double r = 0, g = 0, b = 0;

            if (hsv.S == 0)
            {
                r = hsv.V;
                g = hsv.V;
                b = hsv.V;
            }
            else
            {
                int i;
                double f, p, q, t;

                if (hsv.H == 360)
                    hsv.H = 0;
                else
                    hsv.H = hsv.H / 60;

                i = (int)Math.Truncate(hsv.H);
                f = hsv.H - i;

                p = hsv.V * (1.0 - hsv.S);
                q = hsv.V * (1.0 - (hsv.S * f));
                t = hsv.V * (1.0 - (hsv.S * (1.0 - f)));

                switch (i)
                {
                    case 0:
                        r = hsv.V;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = hsv.V;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = hsv.V;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = hsv.V;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = hsv.V;
                        break;

                    default:
                        r = hsv.V;
                        g = p;
                        b = q;
                        break;
                }

            }

            return Color.FromArgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }


        public static Bitmap ResizeImage(Bitmap imgToResize, Size size)
        {
            Bitmap b = new Bitmap(size.Width, size.Height);
            try
            {
                using (Graphics g = Graphics.FromImage((Image)b))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
                }
                return b;
            }
            catch
            {
                Console.WriteLine("Bitmap could not be smoothly resized");
                return b;
            }
        }




        /// <summary>
        /// Set correct color indexes in target Indexed bitmap using source non-Indexed bitmap and target's Palette
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        unsafe void SetBitmapIndexedColors(Bitmap source,Bitmap target,int nColors)
        {
            if ((source.Width != target.Width) || (source.Height!=target.Height)) throw new Exception("source size and target size must be equal!");
            // set indexed data
            BitmapData sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData targetData = target.LockBits(new Rectangle(0, 0, target.Width, target.Height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

            int* s = (int *)sourceData.Scan0;
            byte* t = (byte*)targetData.Scan0;

            Color[] colors = target.Palette.Entries;
            byte []a = new byte[colors.Length];
            byte []b = new byte[colors.Length];
            byte []r = new byte[colors.Length];
            byte []g = new byte[colors.Length];
            nColors = 0;
            foreach (Color c in colors)
            {
                a[nColors] = c.A;
                b[nColors] = c.B;
                g[nColors] = c.G;
                r[nColors] = c.R;

                if ((nColors > 0) && (c.A == 0)) break;
                nColors++;
            }

            int xt = 0;

            // looking for mixed colors
            HashSet<int> colorset = new HashSet<int>();
            //            for (int q = 0; q < source.Width * source.Height; q++)
            //            {
            //              colorset.Add(s[q]&0xFFFFFF);
            //          }
            // now reducing colors

            Dictionary<int, int> colorsCache = new Dictionary<int, int>();

            for (int q=0;q<source.Width*source.Height;q++)
            {
                //Color c = Color.FromArgb(s[q]);
                int c = s[q];
                int index = 0;
                if (colorsCache.TryGetValue(c, out index))
                {
                }
                else
                {
                    byte bb = (byte)(c & 0xff);
                    byte gg = (byte)((c >> 8) & 0xff);
                    byte rr = (byte)((c >> 16) & 0xff);
                    byte aa = (byte)((c >> 24) & 0xff);
                    int mindist = 255 * 255 * 5;
                    int mini = 0;

                    for (int i = 0; i < nColors; i++)
                    {
                        int dist = ((r[i] - rr) * (r[i] - rr) + (g[i] - gg) * (g[i] - gg) + (b[i] - bb) * (b[i] - bb)) / 3;
                        dist = (a[i] - aa) * (a[i] - aa) / 2 + dist * (1 + a[i] * aa / 255 / 255);

                        if (dist < mindist)
                        {
                            mindist = dist;
                            mini = i;
                        }
                    }

                    index = mini;

                    if (colorsCache.Count<160256)
                     colorsCache.Add(c, index);
                }
                t[xt] = (byte) index;   //(byte) FindClosestColorIndex(c, target.Palette, nColors);
                //colorset.Add(mini);
                xt++;
                if (xt >= source.Width)
                {
                    xt = 0;
                    t+=targetData.Stride;
                }
            }

            // rearange palette

            source.UnlockBits(sourceData);
            target.UnlockBits(targetData);

        }

        double CDistance(Color a, Color b)
        {

            int deltaR = a.R - b.R;
            int deltaG = a.G - b.G;
            int deltaB = a.B - b.B;
            int deltaAlpha = a.A - b.A;

            double rgbDistanceSquared = (deltaR * deltaR + deltaG * deltaG + deltaB * deltaB) / 3;

            return deltaAlpha * deltaAlpha / 2.0 + rgbDistanceSquared * a.A * b.A / (255 * 255);
        }

        double HSVDistance(Color a, Color b)
        {
            double xa = a.GetSaturation() * Math.Sin(a.GetHue());
            double ya = a.GetSaturation() * Math.Cos(a.GetHue());
            double xb = b.GetSaturation() * Math.Sin(b.GetHue());
            double yb = b.GetSaturation() * Math.Cos(b.GetHue());
            double ab = a.GetBrightness();
            double bb = b.GetBrightness();
            return ((xa - xb) * (xa - xb) + (ya - yb) * (ya - yb) + (ab - bb) * (ab - bb));
        }
        double HSDistance(Color a, Color b)
        {
            double xa = a.GetSaturation() * Math.Sin(a.GetHue());
            double ya = a.GetSaturation() * Math.Cos(a.GetHue());
            double xb = b.GetSaturation() * Math.Sin(b.GetHue());
            double yb = b.GetSaturation() * Math.Cos(b.GetHue());
            return ((xa - xb) * (xa - xb) + (ya - yb) * (ya - yb));
        }

        void ReduceColors(List<Color> colors,double threshold)
        {
            bool found = false;
            do
            {
                found = false;
                for (int q=1;q<colors.Count;q++)
                {
                    double mindist = 1E+10;
                    int mincolor=0;
                    for (int i=q+1;i<colors.Count;i++)
                    {
                        double dist = HSVDistance(colors[q], colors[i]);
                        if (dist < mindist)
                        {
                            mindist = dist;
                            mincolor = i;
                        }
                        i++;
                    }
                    if (mindist < threshold)
                    {
                        // combinig
                        colors[q] = Color.FromArgb((colors[q].R + colors[mincolor].R) / 2,
                            (colors[q].G + colors[mincolor].G) / 2,
                            (colors[q].B + colors[mincolor].B) / 2);
                        colors.Remove(colors[mincolor]);
                        found = true;
                    }

                }
            }
            while (found);
        }

        /// <summary>
        /// Reduce number of colors to n maximum distinct
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="n"></param>
        void ReduceColors(List<Color> colors, int n)
        {
            if (n <= 0) return;
            while (colors.Count>n)
            {
                // removing closest points 
                int minfirst = 0;
                int minsecond = 1;
                double mindisttofirst = 1E+10;
                int q = 0;
                for (q = 0; q < colors.Count; q++)
                {
                    double mindist = 1E+10;
                    int mincolor = 0;
                    for (int i = q + 1; i < colors.Count; i++)
                    {
                        double dist = HSVDistance(colors[q], colors[i]);
                        if (dist < mindist)
                        {
                            mindist = dist;
                            mincolor = i;
                        }
                        i++;
                    }
                    if (mindist < mindisttofirst)
                    {
                        minfirst = q;
                        minsecond = mincolor;
                        mindisttofirst = mindist;
                    }
                }

                q = minfirst;
                colors[q] = Color.FromArgb((colors[q].R + colors[minsecond].R) / 2,
                    (colors[q].G + colors[minsecond].G) / 2,
                    (colors[q].B + colors[minsecond].B) / 2);
                colors.Remove(colors[minsecond]);

            }
        }

        List<Color> AddMixedColors(List<Color> colors, double threshold, int MaxAdd)
        { // add mixes of each separate color

            List<Color> distcolors = new List<Color>(colors.Skip(1));
            ReduceColors(distcolors, (int)Math.Sqrt(2*MaxAdd));
            List<Color> newC = new List<Color>(colors);

           for (int q = 0; q < distcolors.Count; q++)
            {
                for (int i = q+1; i < distcolors.Count; i++)
                {
                        newC.Add(Color.FromArgb((distcolors[q].R+ distcolors[i].R)/2,
                        (distcolors[q].G + distcolors[i].G) / 2, (distcolors[q].B + distcolors[i].B) / 2                            ));
                        if (newC.Count >= 256) break;
                }
            }
            return newC.Take(256).ToList();
        }

        public virtual string MakeDjvuPageHtml(DjvuDocument doc,string Path, int n)
        {
            string base64fore=null;
            string base64back=null;
            int W = 0;
            int H = 0;
            bool isback = false;
            bool isfore = false;

            long start = System.Environment.TickCount;

            {
                if ((n < 1) || (n > doc.Pages.Count)) return null;
                W = doc.Pages[n - 1].Width;
                H = doc.Pages[n - 1].Height;

                using (MemoryStream memory = new MemoryStream())
                {
                    using (Bitmap fore = doc.Pages[n - 1].Image.Foreground)
                    {
                        if (fore != null)
                        {
                            isfore = true;

                            using (Bitmap resize = ResizeImage(fore, new Size(fore.Width / 2, fore.Height / 2)))
                            {

                                // downscale image
                                int nColors = 256;// Math.Min(256,1+(fore.Palette.Entries.Length-1)*3);

                                PixelFormat needFormat = PixelFormat.Format8bppIndexed;
                                if ((fore.PixelFormat & PixelFormat.Indexed) != 0)
                                {
                                    nColors = 0;
                                    List<Color> colors = new List<Color>();

                                    foreach (Color c in fore.Palette.Entries)
                                    {
                                        if ((nColors > 0) && (c.A == 0)) break;
                                        colors.Add(c);
                                        nColors++;
                                    }

                                    if (colors.Count > 4)
                                        ReduceColors(colors, 0.0004);

                                    if (colors.Count > 85) // need 32bpp format
                                    {
                                        resize.Save(memory, ImageFormat.Png);

                                    }
                                    else
                                        using (Bitmap thumb = new Bitmap(fore.Width / 2, fore.Height / 2, needFormat))
                                        {
                                            ColorPalette baseColors = fore.Palette;
                                            ColorPalette newColors = thumb.Palette;
                                            // TODO: reduce base colors deleting closest,
                                            //    now using only 255/3 first colors
                                            //if (fore.Palette.Entries.Length < 85)
                                            //{

                                            // reduce colors deleteing similar
                                            // thresold 2pi*100^2+30^2

                                            if ((colors.Count > 2) && (colors.Count <= 85)) // n' = 1+(n-1)*(n-2)/2+n-1, must be < 86
                                                colors = AddMixedColors(colors, 0.04, 86 - colors.Count);
                                            nColors = colors.Count; // must be < 86

                                            nColors = Math.Min(256, 1 + (nColors - 1) * 3);


                                            int q;
                                            for (q = 0; q < newColors.Entries.Length; q++)
                                                newColors.Entries[q] = Color.FromArgb(0);

                                            newColors.Entries[0] = baseColors.Entries[0];

                                            q = 0;
                                            foreach (Color color in colors)
                                            {
                                                if (q == 0) { q = 1; continue; }

                                                // add 2 semitransparent colors
                                                Color light = Color.FromArgb(170, color.R, color.G, color.B);
                                                Color dark = Color.FromArgb(85, color.R, color.G, color.B);
                                                newColors.Entries[q] = color;
                                                newColors.Entries[q + 1] = light;
                                                newColors.Entries[q + 2] = dark;
                                                q += 3;
                                                // add mixing colors:

                                                if (q >= nColors) break;
                                            }
                                            thumb.Palette = newColors;
                                            //}
                                            //else thumb.Palette = fore.Palette;

                                            SetBitmapIndexedColors(resize, thumb, nColors);
                                                // resize.Save(memory, ImageFormat.Png);

                                            thumb.Save(memory, ImageFormat.Png);
                                        }
                                }
                                else
                                {
                                     resize.Save(memory, ImageFormat.Png);

                                }
                            }

                            //  fore.Save(memory, ImageFormat.Png);
                            /*
                        memory.Seek(0, SeekOrigin.Begin);

                        using (var magick = new MagickImage(memory))
                        {
                            QuantizeSettings qSetting = new QuantizeSettings();
                            qSetting.ColorSpace = ColorSpace.Transparent;
                            qSetting.DitherMethod = DitherMethod.Riemersma;
                            qSetting.Colors = nColors;
                            magick.Quantize(qSetting);
                            magick.Write($"{Path}/page{n}.fore.png", MagickFormat.Png8,);
                        }
                        */
                        using (Stream f = File.Create($"{Path}/page{n}.fore.png"))
                            {
                                memory.WriteTo(f);
                            }

                            //}
                            //Console.WriteLine("Pure FORE takes " + (System.Environment.TickCount - start) + "ms");

                            //base64fore = Convert.ToBase64String(memory.ToArray());
                        }
                    }
                }

               // Console.WriteLine("FORE takes " + (System.Environment.TickCount - start) + "ms");

                using (MemoryStream memory = new MemoryStream())
                {
                    using (Bitmap back = doc.Pages[n - 1].Image.Background)
                    {
                        if (back != null)
                        {
                            isback = true;
                            EncoderParameters myEncoderParameters = new EncoderParameters(1);
                            EncoderParameter myEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 75L);
                            myEncoderParameters.Param[0] = myEncoderParameter;
                            back.Save(memory, GetEncoder(ImageFormat.Jpeg), myEncoderParameters);
                            using (Stream f = File.OpenWrite($"{Path}/page{n}.back.jpg"))
                            {
                                memory.WriteTo(f);
                            }
                            
//                            base64back = Convert.ToBase64String(memory.ToArray());
                        }
                    }
                }

            }

            Console.WriteLine("IMG takes " + (System.Environment.TickCount - start) + "ms");

            // forming html output
            String output = new StringBuilder("<div class='pf'>").Append(isfore ? "" : $"<img id='back' width='{W}' height='{H}' src='page{n}.back.jpg'>").
                Append($"<img id='fore' width='{W}' height='{H}' src='page{n}.fore.png' ").
                Append(isback ? $"style='background: url(page{n}.back.jpg); background-size:cover;' />" : "/>")
                .Append("</div>").ToString();

            // TODO: text layer
            /*
            if (base64back != null)
                output += $"<img id=\"back\" width=\"{W}\" height=\"{H}\" datasize={base64back.Length} src=\"data:image/jpeg;base64," + base64back + "\"/>";
            if (base64fore != null)
                output += $"<img id=\"fore\" width=\"{W}\" height=\"{H}\" datasize={base64fore.Length} src=\"data:image/png;base64," + base64fore + "\"/>";
*/
            return output;
        }
       
        public static void GZipFile(string file, bool delOrig=true)
        {
            if (File.Exists(file))
            using (FileStream f = File.Create(file + "z"))
            {
                using (GZipStream stream = new GZipStream(f, CompressionLevel.Optimal))
                {
                    using (FileStream fs = File.OpenRead(file))
                    {
                        fs.CopyTo(stream);
                    }
                }
            }
            if (delOrig)
            File.Delete(file);
        }

        public virtual DocTitlePageType AnalizePageType(Bitmap bmp)
        {
            const int N = 10000;
            Random rand = new Random();
            int nWhite=0;
            int nBlack=0;
            int nColor = 0;
            double nGrad = 0;
            for (int n = 0; n < N; n++)
            {
                int x = (int) ((bmp.Width-1) * rand.NextDouble());
                int y = (int)((bmp.Height-1) * rand.NextDouble());
                Color c = bmp.GetPixel(x, y);
                Color c2 = bmp.GetPixel(x+1, y);
                Color c3 = bmp.GetPixel(x, y+1);
                nGrad += (Math.Abs(c3.GetBrightness() - c.GetBrightness()) + Math.Abs(c2.GetBrightness() - c.GetBrightness()));
                float b = c.GetBrightness();
                if (c.GetSaturation() > 0.15)
                {
                    nColor++;
                }
                else
                {
                    if (b > 0.8) nWhite++;
                    if (b < 0.3) nBlack++;
                }
            }
            //nGrad /= N;
            if (nColor>0.2*N)
            {
                return DocTitlePageType.Color;
            }
            if (nBlack>0.7*nWhite) // strange: white on black
                return DocTitlePageType.Color;
            if (nGrad/2 < 0.5 * nBlack) // large letters or graphics
                return DocTitlePageType.BWTitle;

            return DocTitlePageType.BWText;
        }


        public string GetDocHtml(string dir)
        {
            try
            {
                if (Directory.Exists(dir + "META-INF"))
                {
                    string opfpath = "content.opf";
                    try
                    {
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.Load(dir + "META-INF/container.xml");
                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
                        nsmgr.AddNamespace("main", "urn:oasis:names:tc:opendocument:xmlns:container");
                        opfpath = xmlDocument.SelectSingleNode("//main:rootfile", nsmgr).Attributes["full-path"].Value;
                    }
                    catch (Exception e) { }
                    return (opfpath);
                }

                /*
                using (Stream f = File.OpenRead(dir + ".html.gz"))
                {
                    using (GZipStream gz = new GZipStream(f, CompressionMode.Decompress))
                    {
                        using (var resultStream = new MemoryStream())
                        {
                            gz.CopyTo(resultStream);

                            resultStream.Seek(0, SeekOrigin.Begin);
                            using (StreamReader r = new StreamReader(resultStream))
                            {

                                string ret = "";
                                string a;
                                while ((a = r.ReadLine())!=null)
                                {
                                    if ((a.Length > 0) && (a[0] == '.'))
                                        a = a.Insert(0, "#page-container ");
                                    ret += a+'\n';
                                }
                                // adding class to css
                                return ret;


                            }
                        }
                    }
                }
                */
                return File.ReadAllText(dir + ".html");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading html " + dir + ":" + e.Message);
            }

            return null;
        }

        ConcurrentQueue<(string file, Action<string, ProcessResult, UserContext> done)> Queue = new ConcurrentQueue<(string, Action<string, ProcessResult, UserContext>)>();


        public void ProcessDocument(string filename, Action<string, ProcessResult, UserContext> onfinish=null)
        {
            Queue.Enqueue((filename,onfinish));

        }

    }
}
