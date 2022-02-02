package jsonserialization

import (
	"encoding/base64"
	"strconv"
	"strings"
	"time"

	"github.com/google/uuid"

	absser "github.com/microsoft/kiota/abstractions/go/serialization"
)

// JsonSerializationWriter implements SerializationWriter for JSON.
type JsonSerializationWriter struct {
	writer []string
}

// NewJsonSerializationWriter creates a new instance of the JsonSerializationWriter.
func NewJsonSerializationWriter() *JsonSerializationWriter {
	return &JsonSerializationWriter{
		writer: make([]string, 0),
	}
}
func (w *JsonSerializationWriter) writeRawValue(value string) {
	w.writer = append(w.writer, value)
}
func (w *JsonSerializationWriter) writeStringValue(value string) {
	w.writeRawValue("\"" + value + "\"")
}
func (w *JsonSerializationWriter) writePropertyName(key string) {
	w.writeRawValue("\"" + key + "\":")
}
func (w *JsonSerializationWriter) writePropertySeparator() {
	w.writeRawValue(",")
}
func (w *JsonSerializationWriter) trimLastPropertySeparator() {
	writerLen := len(w.writer)
	if writerLen > 0 && w.writer[writerLen-1] == "," {
		w.writer = w.writer[:writerLen-1]
	}
}
func (w *JsonSerializationWriter) writeArrayStart() {
	w.writeRawValue("[")
}
func (w *JsonSerializationWriter) writeArrayEnd() {
	w.writeRawValue("]")
}
func (w *JsonSerializationWriter) writeObjectStart() {
	w.writeRawValue("{")
}
func (w *JsonSerializationWriter) writeObjectEnd() {
	w.writeRawValue("}")
}

// WriteStringValue writes a String value to underlying the byte array.
func (w *JsonSerializationWriter) WriteStringValue(key string, value *string) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeStringValue(*value)
	}
	if key != "" && value != nil {
		w.writePropertySeparator()
	}
	return nil
}

// WriteBoolValue writes a Bool value to underlying the byte array.
func (w *JsonSerializationWriter) WriteBoolValue(key string, value *bool) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue(strconv.FormatBool(*value))
	}
	if key != "" && value != nil {
		w.writePropertySeparator()
	}
	return nil
}

// WriteInt32Value writes a Int32 value to underlying the byte array.
func (w *JsonSerializationWriter) WriteInt32Value(key string, value *int32) error {
	if value != nil {
		cast := int64(*value)
		return w.WriteInt64Value(key, &cast)
	}
	return nil
}

// WriteInt64Value writes a Int64 value to underlying the byte array.
func (w *JsonSerializationWriter) WriteInt64Value(key string, value *int64) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue(strconv.FormatInt(*value, 10))
	}
	if key != "" && value != nil {
		w.writePropertySeparator()
	}
	return nil
}

// WriteFloat32Value writes a Float32 value to underlying the byte array.
func (w *JsonSerializationWriter) WriteFloat32Value(key string, value *float32) error {
	if value != nil {
		cast := float64(*value)
		return w.WriteFloat64Value(key, &cast)
	}
	return nil
}

// WriteFloat64Value writes a Float64 value to underlying the byte array.
func (w *JsonSerializationWriter) WriteFloat64Value(key string, value *float64) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue(strconv.FormatFloat(*value, 'f', -1, 64))
	}
	if key != "" && value != nil {
		w.writePropertySeparator()
	}
	return nil
}

// WriteTimeValue writes a Time value to underlying the byte array.
func (w *JsonSerializationWriter) WriteTimeValue(key string, value *time.Time) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue((*value).String())
	}
	if key != "" && value != nil {
		w.writePropertySeparator()
	}
	return nil
}

// WriteISODurationValue writes a ISODuration value to underlying the byte array.
func (w *JsonSerializationWriter) WriteISODurationValue(key string, value *absser.ISODuration) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue((*value).String())
	}
	if key != "" && value != nil {
		w.writePropertySeparator()
	}
	return nil
}

// WriteTimeOnlyValue writes a TimeOnly value to underlying the byte array.
func (w *JsonSerializationWriter) WriteTimeOnlyValue(key string, value *absser.TimeOnly) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue((*value).String())
	}
	if key != "" && value != nil {
		w.writePropertySeparator()
	}
	return nil
}

// WriteDateOnlyValue writes a DateOnly value to underlying the byte array.
func (w *JsonSerializationWriter) WriteDateOnlyValue(key string, value *absser.DateOnly) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue((*value).String())
	}
	if key != "" && value != nil {
		w.writePropertySeparator()
	}
	return nil
}

// WriteUUIDValue writes a UUID value to underlying the byte array.
func (w *JsonSerializationWriter) WriteUUIDValue(key string, value *uuid.UUID) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeStringValue((*value).String())
	}
	if key != "" && value != nil {
		w.writePropertySeparator()
	}
	return nil
}

// WriteByteArrayValue writes a ByteArray value to underlying the byte array.
func (w *JsonSerializationWriter) WriteByteArrayValue(key string, value []byte) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeStringValue(base64.StdEncoding.EncodeToString(value))
	}
	if key != "" && value != nil {
		w.writePropertySeparator()
	}
	return nil
}

// WriteObjectValue writes a Parsable value to underlying the byte array.
func (w *JsonSerializationWriter) WriteObjectValue(key string, item absser.Parsable) error {
	if !item.IsNil() {
		if key != "" {
			w.writePropertyName(key)
		}
		//TODO onBefore for backing store
		w.writeObjectStart()
		//TODO onStart for backing store
		err := item.Serialize(w)
		//TODO onAfter for backing store
		if err != nil {
			return err
		}
		w.trimLastPropertySeparator()
		w.writeObjectEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfObjectValues writes a collection of Parsable values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfObjectValues(key string, collection []absser.Parsable) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteObjectValue("", item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfStringValues writes a collection of String values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfStringValues(key string, collection []string) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteStringValue("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfInt32Values writes a collection of Int32 values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfInt32Values(key string, collection []int32) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteInt32Value("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfInt64Values writes a collection of Int64 values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfInt64Values(key string, collection []int64) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteInt64Value("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfFloat32Values writes a collection of Float32 values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfFloat32Values(key string, collection []float32) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteFloat32Value("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfFloat64Values writes a collection of Float64 values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfFloat64Values(key string, collection []float64) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteFloat64Value("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfTimeValues writes a collection of Time values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfTimeValues(key string, collection []time.Time) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteTimeValue("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfISODurationValues writes a collection of ISODuration values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfISODurationValues(key string, collection []absser.ISODuration) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteISODurationValue("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfTimeOnlyValues writes a collection of TimeOnly values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfTimeOnlyValues(key string, collection []absser.TimeOnly) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteTimeOnlyValue("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfDateOnlyValues writes a collection of DateOnly values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfDateOnlyValues(key string, collection []absser.DateOnly) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteDateOnlyValue("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfUUIDValues writes a collection of UUID values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfUUIDValues(key string, collection []uuid.UUID) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteUUIDValue("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// WriteCollectionOfBoolValues writes a collection of Bool values to underlying the byte array.
func (w *JsonSerializationWriter) WriteCollectionOfBoolValues(key string, collection []bool) error {
	if collection != nil { // empty collections are meaningful
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WriteBoolValue("", &item)
			if err != nil {
				return err
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
		w.writeArrayEnd()
		if key != "" {
			w.writePropertySeparator()
		}
	}
	return nil
}

// GetSerializedContent returns the resulting byte array from the serialization writer.
func (w *JsonSerializationWriter) GetSerializedContent() ([]byte, error) {
	resultStr := strings.Join(w.writer, "")
	return []byte(resultStr), nil
}

// WriteAdditionalData writes additional data to underlying the byte array.
func (w *JsonSerializationWriter) WriteAdditionalData(value map[string]interface{}) error {
	if len(value) != 0 {
		for key, value := range value {
			p, ok := value.(absser.Parsable)
			if ok {
				err := w.WriteObjectValue(key, p)
				if err != nil {
					return err
				}
				continue
			}
			c, ok := value.([]absser.Parsable)
			if ok {
				err := w.WriteCollectionOfObjectValues(key, c)
				if err != nil {
					return err
				}
				continue
			}
			sc, ok := value.([]string)
			if ok {
				err := w.WriteCollectionOfStringValues(key, sc)
				if err != nil {
					return err
				}
				continue
			}
			bc, ok := value.([]bool)
			if ok {
				err := w.WriteCollectionOfBoolValues(key, bc)
				if err != nil {
					return err
				}
				continue
			}
			i32c, ok := value.([]int32)
			if ok {
				err := w.WriteCollectionOfInt32Values(key, i32c)
				if err != nil {
					return err
				}
				continue
			}
			i64c, ok := value.([]int64)
			if ok {
				err := w.WriteCollectionOfInt64Values(key, i64c)
				if err != nil {
					return err
				}
				continue
			}
			f32c, ok := value.([]float32)
			if ok {
				err := w.WriteCollectionOfFloat32Values(key, f32c)
				if err != nil {
					return err
				}
				continue
			}
			f64c, ok := value.([]float64)
			if ok {
				err := w.WriteCollectionOfFloat64Values(key, f64c)
				if err != nil {
					return err
				}
				continue
			}
			uc, ok := value.([]uuid.UUID)
			if ok {
				err := w.WriteCollectionOfUUIDValues(key, uc)
				if err != nil {
					return err
				}
				continue
			}
			tc, ok := value.([]time.Time)
			if ok {
				err := w.WriteCollectionOfTimeValues(key, tc)
				if err != nil {
					return err
				}
				continue
			}
			dc, ok := value.([]absser.ISODuration)
			if ok {
				err := w.WriteCollectionOfISODurationValues(key, dc)
				if err != nil {
					return err
				}
				continue
			}
			toc, ok := value.([]absser.TimeOnly)
			if ok {
				err := w.WriteCollectionOfTimeOnlyValues(key, toc)
				if err != nil {
					return err
				}
				continue
			}
			doc, ok := value.([]absser.DateOnly)
			if ok {
				err := w.WriteCollectionOfDateOnlyValues(key, doc)
				if err != nil {
					return err
				}
				continue
			}
			sv, ok := value.(*string)
			if ok {
				err := w.WriteStringValue(key, sv)
				if err != nil {
					return err
				}
				continue
			}
			bv, ok := value.(*bool)
			if ok {
				err := w.WriteBoolValue(key, bv)
				if err != nil {
					return err
				}
				continue
			}
			i32v, ok := value.(*int32)
			if ok {
				err := w.WriteInt32Value(key, i32v)
				if err != nil {
					return err
				}
				continue
			}
			i64v, ok := value.(*int64)
			if ok {
				err := w.WriteInt64Value(key, i64v)
				if err != nil {
					return err
				}
				continue
			}
			f32v, ok := value.(*float32)
			if ok {
				err := w.WriteFloat32Value(key, f32v)
				if err != nil {
					return err
				}
				continue
			}
			f64v, ok := value.(*float64)
			if ok {
				err := w.WriteFloat64Value(key, f64v)
				if err != nil {
					return err
				}
				continue
			}
			uv, ok := value.(*uuid.UUID)
			if ok {
				err := w.WriteUUIDValue(key, uv)
				if err != nil {
					return err
				}
				continue
			}
			tv, ok := value.(*time.Time)
			if ok {
				err := w.WriteTimeValue(key, tv)
				if err != nil {
					return err
				}
				continue
			}
			dv, ok := value.(*absser.ISODuration)
			if ok {
				err := w.WriteISODurationValue(key, dv)
				if err != nil {
					return err
				}
				continue
			}
			tov, ok := value.(*absser.TimeOnly)
			if ok {
				err := w.WriteTimeOnlyValue(key, tov)
				if err != nil {
					return err
				}
				continue
			}
			dov, ok := value.(*absser.DateOnly)
			if ok {
				err := w.WriteDateOnlyValue(key, dov)
				if err != nil {
					return err
				}
				continue
			}
			ba, ok := value.([]byte)
			if ok {
				err := w.WriteByteArrayValue(key, ba)
				if err != nil {
					return err
				}
				continue
			}
		}
		w.trimLastPropertySeparator()
	}
	return nil
}

// Close clears the internal buffer.
func (w *JsonSerializationWriter) Close() error {
	return nil
}
