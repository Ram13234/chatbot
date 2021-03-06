// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Bot.Connector
{
    using Microsoft.Rest;
    
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Attachments operations.
    /// </summary>
    public partial interface IAttachments
    {
        /// <summary>
        /// GetAttachmentInfo
        /// </summary>
        /// <remarks>
        /// Get AttachmentInfo structure describing the attachment views
        /// </remarks>
        /// <param name='attachmentId'>
        /// attachment id
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="ErrorResponseException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="Microsoft.Rest.SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        Task<HttpOperationResponse<AttachmentInfo>> GetAttachmentInfoWithHttpMessagesAsync(string attachmentId, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));
        /// <summary>
        /// GetAttachment
        /// </summary>
        /// <remarks>
        /// Get the named view as binary content
        /// </remarks>
        /// <param name='attachmentId'>
        /// attachment id
        /// </param>
        /// <param name='viewId'>
        /// View id from attachmentInfo
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="ErrorResponseException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="Microsoft.Rest.SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        Task<HttpOperationResponse<Stream>> GetAttachmentWithHttpMessagesAsync(string attachmentId, string viewId, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
