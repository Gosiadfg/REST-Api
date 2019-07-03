using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace WebApplication2.Controllers
{
    public class Score
    {
        public int UserId { get; set; }
        public int QuizId { get; set; }
        public int ScoreValue { get; set; }
    }

    public class WynikiController : ApiController
    {
        private const string CacheKey = "ScoreStore";

        public WynikiController()
        {
            var ctx = HttpContext.Current;

            if (ctx != null)
            {
                if (ctx.Cache[CacheKey] == null)
                {
                    var scores = new Score[]
                    {
                        new Score
                        {
                            UserId = 1, QuizId = 1, ScoreValue = 10
                        },
                        new Score
                        {
                            UserId = 2, QuizId = 2, ScoreValue = 20
                        }
                    };
                    ctx.Cache[CacheKey] = scores;
                }
            }
        }

        // GET api/scores
        public HttpResponseMessage Get(int pageNumber, int pageSize)
        {
            var ctx = HttpContext.Current;
            var result = stronicowanie((Score[])ctx.Cache[CacheKey], pageNumber, pageSize);
            var response = Request.CreateResponse(System.Net.HttpStatusCode.OK, result);
            response.Headers.Add("ETag", String.Format("\"{0}\"", etagheader.etag));
            return response;
        }

        // GET api/scores/5
        public HttpResponseMessage Get(int id)
        {
            var ctx = HttpContext.Current;
            List<Score>scores = new List<Score>();

            foreach (Score score in (Score[])ctx.Cache[CacheKey])
            {
                if (score.QuizId == id)
                {
                    scores.Add(score);
                }
            }
            var response = Request.CreateResponse(System.Net.HttpStatusCode.OK, scores.ToArray());
            response.Headers.Add("ETag", String.Format("\"{0}\"", etagheader.etag));
            return response;
        }

        private bool findduplicate(Score score)
        {
            var ctx = HttpContext.Current;
            foreach (Score tmp in (Score[])ctx.Cache[CacheKey])
            {
                if (tmp.QuizId == score.QuizId && tmp.UserId == score.UserId && tmp.ScoreValue == score.ScoreValue) return false;
            }
            return true;
        }

        // POST api/wyniki
        public HttpResponseMessage Post(int userid, int quizid, int scorevalue)
        {
            Score score = new Score { UserId = userid, QuizId = quizid, ScoreValue = scorevalue};
            var status = findduplicate(score);

            if (status == true)
            {
                SaveScore(score);
                var response = Request.CreateResponse<Score>(System.Net.HttpStatusCode.Created, score);
                response.Headers.Add("ETag", String.Format("\"{0}\"", etagheader.etag));
                return response;
            }
            else return Request.CreateResponse<Score>(System.Net.HttpStatusCode.Conflict, null); //409            
        }

        private bool SaveScore(Score score)
        {
            var ctx = HttpContext.Current;

            if (ctx != null)
            {
                try
                {
                    var currentData = ((Score[])ctx.Cache[CacheKey]).ToList();
                    currentData.Add(score);
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

        public bool DeleteScore(int Id)
        {
            var ctx = HttpContext.Current;

            if (ctx != null)
            {
                try
                {
                    var currentData = ((Score[])ctx.Cache[CacheKey]).ToList();

                    foreach (Score score in (Score[])ctx.Cache[CacheKey])
                    {
                        if (score.QuizId == Id)
                        {
                            currentData.Remove(score);
                            ctx.Cache[CacheKey] = currentData.ToArray();
                        }
                    }
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

        public Score[] stronicowanie(Score[] list, int pageNumber, int pageSize)
        {
            int count = list.Count();
            int TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            var items = list.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return items.ToArray();
        }
    }
}