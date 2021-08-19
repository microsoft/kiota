package abstractions

type HttpMethod int

const (
	GET HttpMethod = iota
	POST
	PATCH
	DELETE
	OPTIONS
	CONNECT
	PUT
	TRACE
	HEAD
)

func (m HttpMethod) String() string {
	return []string{"GET", "POST", "PATCH", "DELETE", "OPTIONS", "CONNECT", "PUT", "TRACE", "HEAD"}[m]
}
