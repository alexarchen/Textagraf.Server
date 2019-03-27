using Docodo;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SearchServer.Models
{
    // Model representation of index
    public class SessionIndexModel 
    {
        Index index;
        public SessionIndexModel(Index index)
        {
            this.index = index;
        }
        public SessionIndexModel()
        {

        }
        [Required(ErrorMessage = "Path must be set")]
        [DataType(DataType.Url)]
        public string Path { get; set; }

        public enum TypeEnum { Web, Mysql};

        public TypeEnum Type;
        public List<String> Languages = new List<string>();
        public bool IsCreating { get => index != null ? index.IsCreating : false; }
        public Index.Status status { get => index!=null?index.status:Index.Status.Idle; }
        public bool CanSearch { get => index!=null?index.CanSearch:false; }
    }
}
