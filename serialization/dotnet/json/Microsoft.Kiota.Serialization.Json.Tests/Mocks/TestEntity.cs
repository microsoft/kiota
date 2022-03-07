using System;
using System.Collections.Generic;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Json.Tests.Mocks
{
    public class TestEntity : IParsable, IAdditionalDataHolder
    {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>Read-only.</summary>
        public string Id { get; set; }
        /// <summary>Read-only.</summary>
        public TestEnum? Numbers { get; set; }
        /// <summary>Read-only.</summary>
        public TimeSpan? WorkDuration { get; set; }
        /// <summary>Read-only.</summary>
        public Date? BirthDay { get; set; }
        /// <summary>Read-only.</summary>
        public Time? StartWorkTime { get; set; }
        /// <summary>Read-only.</summary>
        public Time? EndWorkTime { get; set; }
        /// <summary>Read-only.</summary>
        public DateTimeOffset? CreatedDateTime { get; set; }
        /// <summary>Read-only.</summary>
        public string OfficeLocation { get; set; }
        /// <summary>
        /// Instantiates a new entity and sets the default values.
        /// </summary>
        public TestEntity()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<T, IParseNode>> GetFieldDeserializers<T>()
        {
            return new Dictionary<string, Action<T, IParseNode>> {
                {"id", (o,n) => { (o as TestEntity).Id = n.GetStringValue(); } },
                {"numbers", (o,n) => { (o as TestEntity).Numbers = n.GetEnumValue<TestEnum>(); } },
                {"createdDateTime", (o,n) => { (o as TestEntity).CreatedDateTime = n.GetDateTimeOffsetValue(); } },
                {"officeLocation", (o,n) => { (o as TestEntity).OfficeLocation = n.GetStringValue(); } },
                {"workDuration", (o,n) => { (o as TestEntity).WorkDuration = n.GetTimeSpanValue(); } },
                {"birthDay", (o,n) => { (o as TestEntity).BirthDay = n.GetDateValue(); } },
                {"startWorkTime", (o,n) => { (o as TestEntity).StartWorkTime = n.GetTimeValue(); } },
                {"endWorkTime", (o,n) => { (o as TestEntity).EndWorkTime = n.GetTimeValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        /// </summary>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("id", Id);
            writer.WriteEnumValue<TestEnum>("numbers",Numbers);
            writer.WriteDateTimeOffsetValue("createdDateTime", CreatedDateTime);
            writer.WriteStringValue("officeLocation", OfficeLocation);
            writer.WriteTimeSpanValue("workDuration", WorkDuration);
            writer.WriteDateValue("birthDay", BirthDay);
            writer.WriteTimeValue("startWorkTime", StartWorkTime);
            writer.WriteTimeValue("endWorkTime", EndWorkTime);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
