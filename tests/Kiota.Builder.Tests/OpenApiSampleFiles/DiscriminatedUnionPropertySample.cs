namespace Kiota.Builder.Tests.OpenApiSampleFiles;

public static class DiscriminatedUnionPropertySample
{
    /**
    * An OpenAPI 3.0.0 sample document with a named oneOf schema (WeatherForecast) that uses a
    * discriminator. WeatherSummary is a container model that references WeatherForecast as both
    * a collection property and a single-object property. This validates that the TypeScript
    * deserializer uses the base type factory (createWeatherForecastFromDiscriminatorValue) rather
    * than chaining subtype factories with ??.
    */
    public static readonly string OpenApiYaml = @"
openapi: 3.0.0
info:
  title: Forecast API
  version: 1.0.0
paths:
  /forecast:
    get:
      summary: Get forecast summary
      responses:
        '200':
          description: A weather summary
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/WeatherSummary'
components:
  schemas:
    WeatherForecast:
      oneOf:
        - $ref: '#/components/schemas/RainyDayForecast'
        - $ref: '#/components/schemas/SunnyDayForecast'
      discriminator:
        propertyName: forecastType
        mapping:
          rain: '#/components/schemas/RainyDayForecast'
          sunny: '#/components/schemas/SunnyDayForecast'
    RainyDayForecast:
      type: object
      properties:
        forecastType:
          type: string
        rainAmount:
          type: number
    SunnyDayForecast:
      type: object
      properties:
        forecastType:
          type: string
        uvIndex:
          type: number
    WeatherSummary:
      type: object
      properties:
        forecasts:
          type: array
          items:
            $ref: '#/components/schemas/WeatherForecast'
        primaryForecast:
          $ref: '#/components/schemas/WeatherForecast'";
}
