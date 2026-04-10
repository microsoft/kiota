<?php
namespace Integration\Test;

require __DIR__ . '/../vendor/autoload.php';

use DateTime;

use Integration\Test\Client\ApiClient;
use Integration\Test\Client\Models\WeatherForecast;
use Integration\Test\Client\Models\WeatherForecast_enumValue;

use Microsoft\Kiota\Abstractions\Authentication\AnonymousAuthenticationProvider;
use Microsoft\Kiota\Http\GuzzleRequestAdapter;

use PHPUnit\Framework\TestCase;
class DefaultValuesTest extends TestCase {
    
    public function testDefaultValues() {
        
        $authProvider = new AnonymousAuthenticationProvider();
        $requestAdapter = new GuzzleRequestAdapter($authProvider);
        $requestAdapter->setBaseUrl('http://127.0.0.1:1080');
        $client = new ApiClient($requestAdapter);

        //Call a sample endpoint - not really needed here.
        $serviceResponse = $client->api()->v1()->weatherForecast()->get()->wait();
        $this->assertNotNull($serviceResponse);
        $this->assertEquals(1, sizeof($serviceResponse));

        //Now the real test: create a model class and verify that all properties have the default values.
        $model = new WeatherForecast();
        
        $this->assertEquals(true, $model->getBoolValue());

        $this->assertNotNull($model->getDateOnlyValue());
        $this->assertEquals('1900-01-01', strval($model->getDateOnlyValue()));

        $this->assertNotNull($model->getDateValue());
        //Format the value in ISO8601 (the original value in the model is RFC3339, which has a colon in the timezone offset - this difference is not relevant for the test):
        $this->assertEquals('1900-01-01T00:00:00+0000', $model->getDateValue()->format(DateTime::ISO8601));

        $this->assertNotNull($model->getDateValueLocalTime());
        //Local time in API description is converted to DateTime in local time zone, which depends on the current system. So check only the date/time parts of the value.
        $this->assertStringStartsWith('1900-01-01T00:00:00', $model->getDateValueLocalTime()->format(DateTime::ISO8601));

        $this->assertEquals(25.5, $model->getDecimalValue());
        $this->assertEquals(25.5, $model->getDoubleValue());
        $this->assertEquals(new WeatherForecast_enumValue('one'), $model->getEnumValue());
        $this->assertEquals(25.5, $model->getFloatValue());

        $this->assertNotNull($model->getGuidValue());
        $this->assertEquals('00000000-0000-0000-0000-000000000000', strval($model->getGuidValue()));

        $this->assertEquals(255, $model->getLongValue());
        $this->assertEquals('Test', $model->getSummary());
        $this->assertEquals(15, $model->getTemperatureC());

        $this->assertNotNull($model->getTimeValue());
        $this->assertEquals('00:00:00', strval($model->getTimeValue()));
    }
}
?>