package services;
import java.io.IOException;

public class VersionHandler {
    final KiotaJavaClient client = new KiotaJavaClient();
    public String getVersion() throws IOException {
        return client.createRequest("GetVersion", String.class).execute();
    }
}