import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:microsoft_kiota_http/microsoft_kiota_http.dart';
import 'package:test/test.dart';
import '../src/api_client.dart';
import '../src/models/error.dart';

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
      expect(
          () => client.api.v1.topics.getAsync(),
          throwsA(predicate(
              (e) => e is Error && e.id == 'my-sample-id' && e.code == 123)));
    });
  });
}
