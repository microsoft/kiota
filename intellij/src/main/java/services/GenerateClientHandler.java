package services;
import com.github.arteam.simplejsonrpc.client.builder.RequestBuilder;



public class GenerateClientHandler {

     KiotaJavaClient c;


     public void generateclient(String DescriptionPath, String output, KiotaGenerationLanguage language,String[] include, String[] exclude, String clientclassname, String clientclassnamespace){
         c = new KiotaJavaClient();
         // Create the RequestBuilder
         RequestBuilder<Object> requestBuilder = c.createRequest("Generate", Object.class);
         Object response = requestBuilder.params(
                 DescriptionPath ,output, language, include, include,clientclassname,clientclassnamespace
         ).execute();

         //System.out.println(response);
     }
//        public static void main(String[] args) {
//            String descriptionpath = "C:\\Users\\v-malhossain\\Documents\\Eoutput\\posts-api.yml";
//            String OutputPath = "C:\\Users\\v-malhossain\\Documents\\example\\app\\src\\main\\java\\kiotaposts";
//            KiotaGenerationLanguage lan = KiotaGenerationLanguage.Java;
//            String[] include = new String[0];
//            String[] exclude = new String[0];
//            String clientname = "PostsClient";
//            String clientclassnamespace = "kiotaposts.client";
//
//            GenerateClientHandler handler = new GenerateClientHandler();
//
//            // Call the generateclient method with the provided parameters
//            handler.generateclient(descriptionpath, OutputPath, lan, include, exclude, "PostsClient", "kiotaposts.client");
//
//
//
//        }
}
