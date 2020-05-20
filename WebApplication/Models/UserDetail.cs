using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FirstWebApplication.WebApplication.Models
{
    public class UserDetail
    {
        //public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }

        public override string ToString()
        {
            return $"{Name} {Password} {IsAdmin}";
        }
    }
}