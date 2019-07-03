using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace WebApplication2.Controllers
{
    public static class etagheader
    {
        public static int etag = 1234;
    }

    public class Quiz
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
    }   

    public class QuizesController : ApiController
    {
        
        private const string CacheKey = "QuizStore";

        public QuizesController()
        {
            var ctx = HttpContext.Current;

            if (ctx != null)
            {
                if (ctx.Cache[CacheKey] == null)
                {
                    var quizes = new Quiz[]
                    {
                    new Quiz
                    {
                        Id = 1, UserId = 1,Name = "Quiz1"
                    },
                    new Quiz
                    {
                        Id = 2, UserId = 2,Name = "Quiz2"
                    }
                    };
                    ctx.Cache[CacheKey] = quizes;
                }
            }
        }

        // GET api/quizes
        public HttpResponseMessage Get(int pageNumber, int pageSize)
        {
            var ctx = HttpContext.Current;
            var result = stronicowanie((Quiz[])ctx.Cache[CacheKey], pageNumber, pageSize);
            var response = Request.CreateResponse(System.Net.HttpStatusCode.OK, result);
            response.Headers.Add("ETag", String.Format("\"{0}\"", etagheader.etag));
            return response;
        }

        // GET api/quizes/5
        public HttpResponseMessage Get(int id)
        {
            var ctx = HttpContext.Current;
            var item = find((Quiz[])ctx.Cache[CacheKey], id);
            var response = Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.OK, item);
            response.Headers.Add("ETag", String.Format("\"{0}\"", etagheader.etag));
            return response;
        }

        // GET api/quizes?Name=...
        public HttpResponseMessage Get(String Name)
        {
            var ctx = HttpContext.Current;
            var items = findName((Quiz[])ctx.Cache[CacheKey], Name);
            var response = Request.CreateResponse(System.Net.HttpStatusCode.OK, items);
            response.Headers.Add("ETag", String.Format("\"{0}\"", etagheader.etag));
            return response;
        }

        private Quiz find(Quiz[] list, int id)
        {
            foreach (Quiz quiz in list)
            {
                if (quiz.Id == id) return quiz;
            }
            return null;
        }

        private Quiz[] findName(Quiz[] list, String Name)
        {
            List<Quiz> quizes = new List<Quiz>();

            foreach (Quiz quiz in list)
            {
                if (quiz.Name == Name)
                {
                    quizes.Add(quiz);
                }
            }
            return quizes.ToArray();
        }

        private bool findduplicate(Quiz quiz)
        {
            var ctx = HttpContext.Current;
            foreach (Quiz tmp in (Quiz[])ctx.Cache[CacheKey])
            {
                if (tmp.Id==quiz.Id && tmp.UserId==quiz.UserId && tmp.Name==quiz.Name) return false;
            }
            return true;
        }

        // POST api/quizes
        public HttpResponseMessage Post(int id, int userid, string name)
        {
            Quiz quiz = new Quiz { Id = id, UserId = userid, Name = name };
            var status = findduplicate(quiz);
            
            if (status == true)
            {
                SaveQuiz(quiz);
                var response = Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.Created, quiz); //201
                response.Headers.Add("ETag", String.Format("\"{0}\"", etagheader.etag));
                return response;
            }
            else return Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.Conflict, null); //409
        }

        private bool SaveQuiz(Quiz quiz)
        {
            var ctx = HttpContext.Current;

            if (ctx != null)
            {
                try
                {
                    var currentData = ((Quiz[])ctx.Cache[CacheKey]).ToList();
                    currentData.Add(quiz);
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

        // PUT api/quizes/5
        public HttpResponseMessage Put(int id, [FromBody]String value)
        {
            var ctx = HttpContext.Current;
            Quiz quiz = null;
           
            if (Request.Headers.Contains("If-Match"))
            {
                if (Request.Headers.GetValues("If-Match").First()==etagheader.etag.ToString()){
                    if (Update(id) != null)
                    {
                        etagheader.etag++;
                        return Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.OK, quiz); //200
                    }
                    return Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.NotFound, null); //404
                }
                else return Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.PreconditionFailed, null); //412
            }
            else return Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.Forbidden, null); //403
        }

        public Quiz Update(int id)
        {
            var ctx = HttpContext.Current;
            Quiz quiz = null;

            try
            {
                foreach (Quiz quiz2 in (Quiz[])ctx.Cache[CacheKey])
                {
                    if (quiz2.Id == id)
                    {
                        quiz2.Name = HttpContext.Current.Request.QueryString["Name"];
                        quiz = quiz2;
                        return quiz;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        // DELETE api/quizes/5
        public HttpResponseMessage Delete(int id)
        {
            var ctx = HttpContext.Current;
            var item = find((Quiz[])ctx.Cache[CacheKey], id);

            var status = DeleteQuiz(item);
            WynikiController score = new WynikiController();
            score.DeleteScore(item.Id);

            if (status==true) return Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.OK,null);
            else return Request.CreateResponse<Quiz>(System.Net.HttpStatusCode.NotFound, null);
        }

        private bool DeleteQuiz(Quiz quiz)
        {
            var ctx = HttpContext.Current;

            if (ctx != null)
            {
                try
                {
                    var currentData = ((Quiz[])ctx.Cache[CacheKey]).ToList();
                    currentData.Remove(quiz);
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

        public Quiz[] stronicowanie(Quiz[] list, int pageNumber, int pageSize)
        {
            int count = list.Count();
            int TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            var items = list.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return items.ToArray();
        }
    }
}