using System;
using Microsoft.AspNet.Identity;

namespace FirstWebApplication.WebApplication.Models
{     
    public class ApplicationUser : IUser
    {                   
        public string Id { get; set; }
        public string UserName { get; set; }
        public bool IsAdmin { get; set; }
    }
}