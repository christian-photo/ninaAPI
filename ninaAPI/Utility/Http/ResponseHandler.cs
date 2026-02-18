#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.IO;
using System.Threading.Tasks;
using EmbedIO;
using NINA.Sequencer.Container;
using ninaAPI.Utility.Serialization;

namespace ninaAPI.Utility.Http
{
    public class ResponseHandler
    {
        private readonly ISerializerService serializer;

        private object serializerLock = new object();

        public ResponseHandler(ISerializerService serializerService)
        {
            this.serializer = serializerService;
        }

        public async Task SendObject(IHttpContext context, object obj, int statusCode = 200, string mimeType = MimeType.Json)
        {
            string json;
            lock (serializerLock)
            {
                json = serializer.Serialize(obj);
            }
            await SendRaw(context, json, statusCode, mimeType);
        }

        public async Task SendSequence(IHttpContext context, ISequenceContainer container, int statusCode = 200, string mimeType = MimeType.Json)
        {
            string json;
            lock (serializerLock)
            {
                json = serializer.Serialize(container, true);
            }
            await SendRaw(context, json, statusCode, mimeType);
        }

        public async Task SendRaw(IHttpContext context, string json, int statusCode = 200, string mimeType = MimeType.Json)
        {
            context.Response.ContentType = mimeType;
            context.Response.StatusCode = statusCode;

            string text = json;
            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                await writer.WriteAsync(text);
            }
        }

        public async Task SendBytes(IHttpContext context, byte[] bytes, string mimeType, int statusCode = 200)
        {
            context.Response.ContentType = mimeType;
            context.Response.StatusCode = statusCode;

            await context.Response.OutputStream.WriteAsync(bytes);
        }
    }
}