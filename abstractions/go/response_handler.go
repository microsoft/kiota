package abstractions

// Defines the contract for a response handler.
type ResponseHandler interface {
	//     Callback method that is invoked when a response is received.
	//    Parameters:
	//     - response: The response received. Native response type from the HTTP library
	//      Returns:
	//      - The deserialized object model
	//      - An error if any.
	HandleResponse(response interface{}) (interface{}, error)
}
