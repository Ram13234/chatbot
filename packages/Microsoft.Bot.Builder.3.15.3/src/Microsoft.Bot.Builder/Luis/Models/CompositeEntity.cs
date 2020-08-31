// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
// 
// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Microsoft.Bot.Builder.Luis.Models
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Microsoft.Rest;
    using Microsoft.Rest.Serialization;

    /// <summary>
    /// Luis composite entity. Look at https://www.luis.ai/Help for more
    /// information.
    /// </summary>
    public partial class CompositeEntity
    {
        /// <summary>
        /// Initializes a new instance of the CompositeEntity class.
        /// </summary>
        public CompositeEntity() { }

        /// <summary>
        /// Initializes a new instance of the CompositeEntity class.
        /// </summary>
        public CompositeEntity(string parentType, string value, IList<CompositeChild> children)
        {
            ParentType = parentType;
            Value = value;
            Children = children;
        }

        /// <summary>
        /// Type of parent entity.
        /// </summary>
        [JsonProperty(PropertyName = "parentType")]
        public string ParentType { get; set; }

        /// <summary>
        /// Value for entity extracted by LUIS.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "children")]
        public IList<CompositeChild> Children { get; set; }

        /// <summary>
        /// Validate the object. Throws ValidationException if validation fails.
        /// </summary>
        public virtual void Validate()
        {
            if (ParentType == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "ParentType");
            }
            if (Value == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Value");
            }
            if (Children == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Children");
            }
            if (this.Children != null)
            {
                foreach (var element in this.Children)
                {
                    if (element != null)
                    {
                        element.Validate();
                    }
                }
            }
        }
    }
}
