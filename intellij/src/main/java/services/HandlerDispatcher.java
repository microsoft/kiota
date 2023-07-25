package services;
import com.thetransactioncompany.jsonrpc2.JSONRPC2Request;
import com.thetransactioncompany.jsonrpc2.JSONRPC2Response;
import com.thetransactioncompany.jsonrpc2.server.Dispatcher;
import java.util.*;
import java.util.function.Function;


public class HandlerDispatcher {
    private Dispatcher dispatcher;
    private JSONRPC2Request req;
    private JSONRPC2Response resp;

    String method;

    public HandlerDispatcher() {
        // Create a new JSON-RPC 2.0 request dispatcher
        dispatcher = new Dispatcher();
        dispatcher.register(new kiotaVersionHandler());

    }
    public Dispatcher getDispatcher() {
        return dispatcher;
    }

    public void setReq(JSONRPC2Request theRequest){
      this.req = theRequest;

    }
    public JSONRPC2Request getReq() {
        return req;
    }
    public JSONRPC2Request requestbuilder (String themethod, int therequestID){
        method = themethod;
        int requestID = therequestID;
        return new JSONRPC2Request(method, requestID);
    }
    public String getResp(JSONRPC2Request myreq, Function<JSONRPC2Response, String> extractResponse){
         //extractResponse(dispatcher.process(myreq, null);
         return extractResponse.apply(dispatcher.process(myreq, null));
    }
}
