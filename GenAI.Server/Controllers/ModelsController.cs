using GenAI.Server.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace GenAI.Server.Controllers
{
    [ApiController]
    [Route("v1/models")]
    public class ModelsController : ControllerBase
    {
        private readonly RuntimeModelCache cache;
        private readonly ILogger<ModelsController> logger;

        public ModelsController(RuntimeModelCache cache, ILogger<ModelsController> logger)
        {
            this.cache = cache;
            this.logger = logger;
        }


        [HttpGet]
        public IEnumerable<ModelModel> GetModels()
        {
            return cache.GetModels().Select(x => { 
                return new ModelModel
                {
                    Id = x.Id,
                    Object = "model",
                    Created = x.Created,
                    OwnedBy = "druidai"
                };
            });
        }



        public class ModelModel
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("object")]
            public string Object { get; set; }

            [JsonPropertyName("created")]
            public long Created { get; set; }

            [JsonPropertyName("owned_by")]
            public string OwnedBy { get; set; }
        }







    }
}
