package jsonserialization

import (
	"encoding/base64"
	"strconv"
	"strings"
	"time"

	"github.com/google/uuid"

	absser "github.com/microsoft/kiota/abstractions/go/serialization"
)

type JsonSerializationWriter struct {
	writer []string
}

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

func (w *JsonSerializationWriter) WriteStringValue(key string, value *string) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeStringValue(*value)
		return nil
	}
	if key != "" || value != nil {
		w.writePropertySeparator()
	}
	return nil
}
func (w *JsonSerializationWriter) WriteBoolValue(key string, value *bool) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue(strconv.FormatBool(*value))
		return nil
	}
	if key != "" || value != nil {
		w.writePropertySeparator()
	}
	return nil
}
func (w *JsonSerializationWriter) WriteInt32Value(key string, value *int32) error {
	if value != nil {
		cast := int64(*value)
		return w.WriteInt64Value(key, &cast)
	}
	return nil
}
func (w *JsonSerializationWriter) WriteInt64Value(key string, value *int64) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue(strconv.FormatInt(*value, 10))
		return nil
	}
	if key != "" || value != nil {
		w.writePropertySeparator()
	}
	return nil
}
func (w *JsonSerializationWriter) WriteFloat32Value(key string, value *float32) error {
	if value != nil {
		cast := float64(*value)
		return w.WriteFloat64Value(key, &cast)
	}
	return nil
}
func (w *JsonSerializationWriter) WriteFloat64Value(key string, value *float64) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue(strconv.FormatFloat(*value, 'f', -1, 64))
	}
	if key != "" || value != nil {
		w.writePropertySeparator()
	}
	return nil
}
func (w *JsonSerializationWriter) WriteTimeValue(key string, value *time.Time) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeRawValue((*value).String())
		return nil
	}
	if key != "" || value != nil {
		w.writePropertySeparator()
	}
	return nil
}
func (w *JsonSerializationWriter) WriteUUIDValue(key string, value *uuid.UUID) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeStringValue((*value).String())
		return nil
	}
	if key != "" || value != nil {
		w.writePropertySeparator()
	}
	return nil
}
func (w *JsonSerializationWriter) WriteByteArrayValue(key string, value []byte) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		w.writeStringValue(base64.StdEncoding.EncodeToString(value))
		return nil
	}
	if key != "" || value != nil {
		w.writePropertySeparator()
	}
	return nil
}
func (w *JsonSerializationWriter) WriteObjectValue(key string, item absser.Parsable) error {
	if item != nil {
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
func (w *JsonSerializationWriter) WriteCollectionOfObjectValues(key string, collection []absser.Parsable) error {
	if len(collection) > 0 {
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
func (w *JsonSerializationWriter) WriteCollectionOfStringValues(key string, collection []string) error {
	if len(collection) > 0 {
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
func (w *JsonSerializationWriter) WriteCollectionOfInt32Values(key string, collection []int32) error {
	if len(collection) > 0 {
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
func (w *JsonSerializationWriter) WriteCollectionOfInt64Values(key string, collection []int64) error {
	if len(collection) > 0 {
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
func (w *JsonSerializationWriter) WriteCollectionOfFloat32Values(key string, collection []float32) error {
	if len(collection) > 0 {
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
func (w *JsonSerializationWriter) WriteCollectionOfFloat64Values(key string, collection []float64) error {
	if len(collection) > 0 {
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
func (w *JsonSerializationWriter) WriteCollectionOfTimeValues(key string, collection []time.Time) error {
	if len(collection) > 0 {
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
func (w *JsonSerializationWriter) WriteCollectionOfUUIDValues(key string, collection []uuid.UUID) error {
	if len(collection) > 0 {
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
func (w *JsonSerializationWriter) WriteCollectionOfBoolValues(key string, collection []bool) error {
	if len(collection) > 0 {
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
func (w *JsonSerializationWriter) GetSerializedContent() ([]byte, error) {
	resultStr := strings.Join(w.writer, "")
	return []byte(resultStr), nil
}
func (w *JsonSerializationWriter) WriteAdditionalData(value map[string]interface{}) error {
	if value != nil {
		for key, value := range value {
			p, ok := value.(absser.Parsable)
			if ok {
				err := w.WriteObjectValue("", p)
				if err != nil {
					return err
				}
				continue
			}
			c, ok := value.([]absser.Parsable)
			if ok {
				err := w.WriteCollectionOfObjectValues("", c)
				if err != nil {
					return err
				}
				continue
			}
			sc, ok := value.([]string)
			if ok {
				err := w.WriteCollectionOfStringValues("", sc)
				if err != nil {
					return err
				}
				continue
			}
			bc, ok := value.([]bool)
			if ok {
				err := w.WriteCollectionOfBoolValues("", bc)
				if err != nil {
					return err
				}
				continue
			}
			i32c, ok := value.([]int32)
			if ok {
				err := w.WriteCollectionOfInt32Values("", i32c)
				if err != nil {
					return err
				}
				continue
			}
			i64c, ok := value.([]int64)
			if ok {
				err := w.WriteCollectionOfInt64Values("", i64c)
				if err != nil {
					return err
				}
				continue
			}
			f32c, ok := value.([]float32)
			if ok {
				err := w.WriteCollectionOfFloat32Values("", f32c)
				if err != nil {
					return err
				}
				continue
			}
			f64c, ok := value.([]float64)
			if ok {
				err := w.WriteCollectionOfFloat64Values("", f64c)
				if err != nil {
					return err
				}
				continue
			}
			uc, ok := value.([]uuid.UUID)
			if ok {
				err := w.WriteCollectionOfUUIDValues("", uc)
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
			ba, ok := value.([]byte)
			if ok {
				err := w.WriteByteArrayValue(key, ba)
				if err != nil {
					return err
				}
				continue
			}
			w.writePropertySeparator()
		}
		w.trimLastPropertySeparator()
	}
	return nil
}
func (w *JsonSerializationWriter) Close() error {
	return nil
}
