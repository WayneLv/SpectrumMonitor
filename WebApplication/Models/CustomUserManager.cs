using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

namespace FirstWebApplication.WebApplication.Models
{
    public class CustomUserManager : UserManager<ApplicationUser>
    {
        public virtual List<UserDetail> UserDetails { get; set; } = new List<UserDetail> { };
        private string _pathToFiles;

        public CustomUserManager()
            : base(new CustomUserSore<ApplicationUser>())
        {
            
        }

        public void SetFilePath(string pathToFiles)
        {
            _pathToFiles = pathToFiles;
        }

        public override Task<ApplicationUser> FindAsync(string userName, string password)
        {
            var taskInvoke = Task<ApplicationUser>.Factory.StartNew(() =>
                {
                    try
                    {
                        string[] lines = System.IO.File.ReadAllLines(_pathToFiles);

                        foreach (string line in lines)
                        {

                            string[] info = line.Split(' ');

                            if (info.Length != 3)
                            {
                                // add log?
                                continue;
                            }

                            UserDetails.Add(new UserDetail
                            { Name = info[0], Password = info[1], IsAdmin = Convert.ToBoolean(info[2]) });
                        }
                    }
                    catch { }

                    if (null == UserDetails) return null;

                    foreach (UserDetail user in UserDetails)
                    {
                        if (userName == user.Name && password == user.Password)
                        {
                            return new ApplicationUser {Id=userName, UserName = userName, IsAdmin=user.IsAdmin };
                        }
                    }
                    return null;
                });

            return taskInvoke;
        }
    }
}