// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// REPL session buffer: input, output, or prompt.
    /// </summary>
    [DataContract]
    internal struct BufferBlock
    {
        [DataMember(Name = "kind")]
        internal readonly ReplSpanKind Kind;

        [DataMember(Name = "content")]
        internal readonly string Content;

        internal BufferBlock(ReplSpanKind kind, string content)
        {
            Kind = kind;
            Content = content;
        }

        internal static string Serialize(BufferBlock[] blocks)
        {
            var serializer = new DataContractJsonSerializer(typeof(BufferBlock[]));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, blocks);
                return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            }
        }

        /// <exception cref="InvalidDataException" />
        internal static BufferBlock[] Deserialize(string str)
        {
            var serializer = new DataContractJsonSerializer(typeof(BufferBlock[]));
            try
            {
                var bytes = Encoding.UTF8.GetBytes(str);
                using (var stream = new MemoryStream(bytes))
                {
                    var obj = serializer.ReadObject(stream);
                    return (BufferBlock[])obj;
                }
            }
            catch (Exception e)
            {
                throw new InvalidDataException(e.Message, e);
            }
        }
    }
}
