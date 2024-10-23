import 'package:kiota_abstractions/kiota_abstractions.dart';
import 'package:kiota_http/kiota_http.dart';
import 'package:test/test.dart';
import 'src/api_client.dart';

void main() {
  group('apiclient', () {
    test('basic endpoint test', () {
      final requestAdapter = HttpClientRequestAdapter(
        client: KiotaClientFactory.createClient(),
        authProvider: AnonymousAuthenticationProvider(),
        pNodeFactory: ParseNodeFactoryRegistry.defaultInstance,
        sWriterFactory: SerializationWriterFactoryRegistry.defaultInstance,
      );
      requestAdapter.baseUrl = "http://localhost:1080";
      var client = ApiClient(requestAdapter);

      expect(() => client.api.v1.topics.getAsync(), throwsException);
    });
  });
}
