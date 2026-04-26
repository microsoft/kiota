import 'package:microsoft_kiota_abstractions/microsoft_kiota_abstractions.dart';
import 'package:microsoft_kiota_http/microsoft_kiota_http.dart';
import 'package:test/test.dart';
import '../lib/api_client.dart';
import '../lib/models/weather_forecast.dart';
import '../lib/models/weather_forecast_enum_value.dart';

void main() {
  group('apiclient', () {
    test('basic endpoint test', () async {
      final requestAdapter = HttpClientRequestAdapter(
        client: KiotaClientFactory.createClient(),
        authProvider: AnonymousAuthenticationProvider(),
        pNodeFactory: ParseNodeFactoryRegistry.defaultInstance,
        sWriterFactory: SerializationWriterFactoryRegistry.defaultInstance,
      );
      requestAdapter.baseUrl = "http://localhost:1080";
      var client = ApiClient(requestAdapter);

      //Call a sample endpoint - not really needed here.
      var serviceResponse = await client.api.v1.weatherForecast.getAsync();
      expect(serviceResponse, isNotNull);
      expect(serviceResponse?.length, 1);

      //Now the real test: create a model class and verify that all properties have the default values.
      var model = new WeatherForecast();
      expect(true, model.boolValue);

      expect(model.dateOnlyValue, isNotNull);
      expect(model.dateOnlyValue!.toRfc3339String(),  "1900-01-01"); //from DateOnlyExtensions 

      expect(model.dateValue, isNotNull);
      //default format: the timezone is "Z".
      expect(model.dateValue!.toString(), "1900-01-01 00:00:00.000Z");

      expect(model.decimalValue!, 25.5);
      expect(model.doubleValue!, 25.5);
      expect(model.enumValue!, WeatherForecastEnumValue.one);
      expect(model.floatValue!, 25.5);

      expect(model.guidValue, isNotNull);
      expect(model.guidValue!.toString(), "00000000-0000-0000-0000-000000000000");

      expect(model.longValue!, 255);
      expect(model.summary!, "Test");
      expect(model.temperatureC!, 15);

      expect(model.timeValue, isNotNull);
      expect(model.timeValue!.toRfc3339String(), "00:00:00"); //from TimeOnlyExtensions. Millis are only printed if not "0"
    });
  });
}
