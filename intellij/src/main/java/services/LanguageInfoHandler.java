package services;

import com.github.arteam.simplejsonrpc.client.builder.RequestBuilder;
public class LanguageInfoHandler {
     KiotaJavaClient myClient;
    public  LanguagesInformation InfoForDescription(String descriptionPath) {
        myClient = new KiotaJavaClient();
        RequestBuilder<LanguagesInformation> requestBuilder = myClient.createRequest("InfoForDescription", LanguagesInformation.class);
        return requestBuilder.params(descriptionPath).execute();
    }
}