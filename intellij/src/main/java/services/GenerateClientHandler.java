package services;
import com.github.arteam.simplejsonrpc.client.builder.RequestBuilder;
public class GenerateClientHandler {

    KiotaJavaClient myClient;

    /**
     * A method to generate clientClasses.
     * must pass a list of params where the 0th position must start with a string as the params in the kiota server are positional
     * @param DescriptionPath
     * @param output
     * @param language
     * @param include
     * @param exclude
     * @param clientclassname
     * @param clientclassnamespace
     */
    public void generateclient(String DescriptionPath, String output, KiotaGenerationLanguage language, String[] include, String[] exclude, String clientclassname, String clientclassnamespace) {
        myClient = new KiotaJavaClient();
        // Create the RequestBuilder
        RequestBuilder<Object> requestBuilder = myClient.createRequest("Generate", Object.class);
        Object response = requestBuilder.params(
                DescriptionPath, output, language, include, include, clientclassname, clientclassnamespace
        ).execute();

    }
}