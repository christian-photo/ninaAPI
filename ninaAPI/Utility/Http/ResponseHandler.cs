#region "copyright"

/*
    Copyright © 2026 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ninaAPI.Utility.Serialization;
using SimpleW;

namespace ninaAPI.Utility.Http
{
    public class ResponseHandler
    {
        private readonly ISerializerService serializer;

        private readonly Lock serializerLock = new Lock();

        public ResponseHandler(ISerializerService serializerService)
        {
            this.serializer = serializerService;
        }

        public async Task SendObject(SimpleW.HttpResponse response, object obj, int statusCode = 200, string mimeType = "application/json")
        {
            string json;
            lock (serializerLock)
            {
                json = serializer.Serialize(obj);
            }
            await SendRaw(context, json, statusCode, mimeType);
        }

        public async Task SendSequence(SimpleW.HttpResponse response, object container, int statusCode = 200, string mimeType = "application/json")
        {
            string json;
            lock (serializerLock)
            {
                json = serializer.Serialize(container, true);
            }
            await SendRaw(context, json, statusCode, mimeType);
        }

        public async Task SendRaw(SimpleW.HttpResponse response, string json, int statusCode = 200, string mimeType = "application/json")
        {
            response.Text() = mimeType;

            string text = json;
            using (var writer = new StreamWriter(response.))
            {
                await writer.WriteAsync(text);
            }
        }

        public async Task SendBytes(SimpleW.HttpResponse response, byte[] bytes, string mimeType, int statusCode = 200)
        {
            context.Response.ContentType = mimeType;
            context.Response.StatusCode = statusCode;

            await context.Response.OutputStream.WriteAsync(bytes);
        }
    }
}
