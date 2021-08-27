package abstractions

type ResponseHandler interface {
	HandleResponse(response interface{}) (interface{}, error)
}
