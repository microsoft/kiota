package services;
import com.thetransactioncompany.jsonrpc2.JSONRPC2Request;
import com.thetransactioncompany.jsonrpc2.JSONRPC2Response;
import com.thetransactioncompany.jsonrpc2.server.Dispatcher;
import java.util.*;


public class HandlerDispatcher {
    private Dispatcher dispatcher;
    private JSONRPC2Request req;
    private JSONRPC2Response resp;

    public HandlerDispatcher() {
        // Create a new JSON-RPC 2.0 request dispatcher
        dispatcher = new Dispatcher();
        dispatcher.register(new kiotaVersionHandler());
        //JSONRPC2Request req = new JSONRPC2Request("getkiotaversion", "req-id-01");
        //resp = dispatcher.process(req, null);

    }

    // You can add other methods here to handle JSON-RPC requests or perform other tasks.

    public Dispatcher getDispatcher() {
        return dispatcher;
    }

    public JSONRPC2Request getReq() {
        return req;
    }

    public JSONRPC2Response getResp(req){
        return dispatcher.process(this.req, null);
    }
}
