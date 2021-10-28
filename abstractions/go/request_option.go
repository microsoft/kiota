package abstractions

// Represents a request option.
type RequestOption interface {
	GetKey() RequestOptionKey
}
type RequestOptionKey struct {
	Key string
}
