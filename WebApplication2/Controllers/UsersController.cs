using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace WebApplication2.Controllers
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class UsersController : ApiController
    {
        private const string CacheKey = "UserStore";

        public UsersController()
        {
            var ctx = HttpContext.Current;

            if (ctx != null)
            {
                if (ctx.Cache[CacheKey] == null)
                {
                    var users = new User[]
                    {
                        new User
                        {
                            Id = 1, Name = "Ala Kot"
                        },
                        new User
                        {
                            Id = 2, Name = "Jan Kowalski"
                        }
                    };
                    ctx.Cache[CacheKey] = users;
                }
            }
        }

        // GET api/users
        public HttpResponseMessage Get(int pageNumber, int pageSize)
        {
            var ctx = HttpContext.Current;
            var result = stronicowanie((User[])ctx.Cache[CacheKey], pageNumber, pageSize);
            var response = Request.CreateResponse(System.Net.HttpStatusCode.OK, result);
            response.Headers.Add("ETag", String.Format("\"{0}\"", etagheader.etag));
            return response;
        }

        // GET api/users/5
        public HttpResponseMessage Get(int id)
        {
            var ctx = HttpContext.Current;
            var item = find((User[])ctx.Cache[CacheKey], id);
            var response = Request.CreateResponse<User>(System.Net.HttpStatusCode.OK, item);
            response.Headers.Add("ETag", String.Format("\"{0}\"", etagheader.etag));
            return response;
        }

        private User find(User[] list, int id)
        {
            foreach(User user in list)
            {
                if (user.Id == id) return user;
            }
            return null;
        }

        private bool findduplicate(User user)
        {
            var ctx = HttpContext.Current;
            foreach (User tmp in (User[])ctx.Cache[CacheKey])
            {
                if (tmp.Id == user.Id && tmp.Name == user.Name) return false;
            }
            return true;
        }

        // POST api/users
        public HttpResponseMessage Post(int id, string name)
        {
            User user = new User { Id=id, Name=name};
            var status = findduplicate(user);

            if (status == true)
            {
                SaveUser(user);
                var response = Request.CreateResponse<User>(System.Net.HttpStatusCode.Created, user);
                response.Headers.Add("ETag", String.Format("\"{0}\"", etagheader.etag));
                return response;
            }
            else return Request.CreateResponse<User>(System.Net.HttpStatusCode.Conflict, null); //409           
        }

        private bool SaveUser(User user)
        {
             var ctx = HttpContext.Current;

             if (ctx != null)
             {
                 try
                 {
                     var currentData = ((User[])ctx.Cache[CacheKey]).ToList();
                     currentData.Add(user);
                     ctx.Cache[CacheKey] = currentData.ToArray();

                     return true;
                 }
                 catch (Exception ex)
                 {
                     Console.WriteLine(ex.ToString());
                     return false;
                 }
             }
             return false;
        }

        // PUT api/users/5
        public HttpResponseMessage Put(int id, [FromBody]String value)
        {
            var ctx = HttpContext.Current;
            User user = null;

            if (Request.Headers.Contains("If-Match"))
            {
                if (Request.Headers.GetValues("If-Match").First() == etagheader.etag.ToString())
                {
                    if (Update(id) != null)
                    {
                        etagheader.etag++;
                        return Request.CreateResponse<User>(System.Net.HttpStatusCode.OK, user); //200
                    }
                    return Request.CreateResponse<User>(System.Net.HttpStatusCode.NotFound, null); //404
                }
                else return Request.CreateResponse<User>(System.Net.HttpStatusCode.PreconditionFailed, null); //412
            }
            else return Request.CreateResponse<User>(System.Net.HttpStatusCode.Forbidden, null); //403
        }

        public User Update(int id)
        {
            var ctx = HttpContext.Current;
            User user = null;

            try
            {
                foreach (User user2 in (User[])ctx.Cache[CacheKey])
                {
                    if (user2.Id == id)
                    {
                        user2.Name = HttpContext.Current.Request.QueryString["Name"];
                        user = user2;
                        return user;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        // DELETE api/users/5
        public HttpResponseMessage Delete(int id)
        {
            var ctx = HttpContext.Current;
            var item = find((User[])ctx.Cache[CacheKey], id);

            var status = DeleteUser(item);
            WynikiController score = new WynikiController();
            score.DeleteScore(id);

            if (status == true) return Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.OK, null);
            else return Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.NotFound, null);
        }

        private bool DeleteUser(User user)
        {
            var ctx = HttpContext.Current;

            if (ctx != null)
            {
                try
                {
                    var currentData = ((User[])ctx.Cache[CacheKey]).ToList();
                    currentData.Remove(user);
                    ctx.Cache[CacheKey] = currentData.ToArray();

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            return false;
        }

        public User[] stronicowanie(User[] list, int pageNumber, int pageSize)
        {
            int count = list.Count();
            int TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            var items = list.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return items.ToArray();
        }
    }
}