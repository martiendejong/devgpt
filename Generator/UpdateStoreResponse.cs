﻿using System.Text.Json.Serialization;

namespace DevGPT.NewAPI
{
    public class UpdateStoreResponse : ChatResponse<UpdateStoreResponse>
    {
        public List<ModifyDocumentRequest> Modifications { get; set; }
        public List<DeleteDocumentRequest> Deletions { get; set; }
        public string ResponseMessage { get; set; }

        [JsonIgnore]
        public override UpdateStoreResponse _example => new UpdateStoreResponse()
        {
            Modifications = new List<ModifyDocumentRequest>() { new ModifyDocumentRequest { Name = "Name of the document, ie. My Personal File", Path = "The relative path, ie. info\\personalfile.txt", Contents = "The (updated) contents of the file." } },
            Deletions = new List<DeleteDocumentRequest>() { new DeleteDocumentRequest { Path = "The relative path, ie. info\\olddocument.txt" } },
            ResponseMessage = "The response to the user"
        };

        [JsonIgnore]
        public override string _signature => @"{Modifications: [] or null, Deletions: [] or null, ResponseMessage: string}";
    }
}