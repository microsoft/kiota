package jsonserialization

import (
	"encoding/base64"
	"errors"
	"reflect"
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
	w.writer[len(w.writer)] = value
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

func (w *JsonSerializationWriter) WritePrimitiveValue(key string, value interface{}) error {
	if key != "" && value != nil {
		w.writePropertyName(key)
	}
	if value != nil {
		s, ok := value.(*string)
		written := false
		if ok {
			w.writeStringValue(*s)
			written = true
		}
		b, ok := value.(*bool)
		if ok {
			w.writer[len(w.writer)] = strconv.FormatBool(*b)
			written = true
		}
		i32, ok := value.(*int32)
		if ok {
			w.writer[len(w.writer)] = strconv.FormatInt(int64(*i32), 10)
			written = true
		}
		i64, ok := value.(*int64)
		if ok {
			w.writer[len(w.writer)] = strconv.FormatInt(*i64, 10)
			written = true
		}
		f32, ok := value.(*float32)
		if ok {
			w.writer[len(w.writer)] = strconv.FormatFloat(float64(*f32), 'f', -1, 64)
			written = true
		}
		f64, ok := value.(*float64)
		if ok {
			w.writer[len(w.writer)] = strconv.FormatFloat(*f64, 'f', -1, 64)
			written = true
		}
		t, ok := value.(*time.Time)
		if ok {
			w.writer[len(w.writer)] = (*t).String()
			written = true
		}
		u, ok := value.(*uuid.UUID)
		if ok {
			w.writeStringValue((*u).String())
			written = true
		}
		ba, ok := value.([]byte)
		if ok {
			w.writeStringValue(base64.StdEncoding.EncodeToString(ba))
			written = true
		}
		e, ok := value.(*int)
		if ok {
			m, ok := reflect.TypeOf(*e).MethodByName("String")
			if ok {
				res := m.Func.Call([]reflect.Value{})
				if len(res) > 0 {
					w.writeStringValue(res[0].String())
					written = true
				}
			}
		}
		if !written {
			return errors.New("unsupported type for property " + key)
		}
	} else if key != "" {
		w.writeRawValue("null")
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
func (w *JsonSerializationWriter) WriteCollectionOfPrimitiveValues(key string, collection []interface{}) error {
	if len(collection) > 0 {
		if key != "" {
			w.writePropertyName(key)
		}
		w.writeArrayStart()
		for _, item := range collection {
			err := w.WritePrimitiveValue("", item)
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
			}
			c, ok := value.([]absser.Parsable)
			if ok {
				err := w.WriteCollectionOfObjectValues("", c)
				if err != nil {
					return err
				}
			}
			pv, ok := value.([]interface{})
			if ok {
				err := w.WriteCollectionOfPrimitiveValues("", pv)
				if err != nil {
					return err
				}
			} else {
				err := w.WritePrimitiveValue(key, value)
				if err != nil {
					return err
				}
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
