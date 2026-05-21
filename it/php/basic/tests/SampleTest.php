<?php
namespace Integration\Test;

require __DIR__ . '/../vendor/autoload.php';

use Microsoft\Kiota\Abstractions\Authentication\AnonymousAuthenticationProvider;
use Microsoft\Kiota\Http\GuzzleRequestAdapter;

use PHPUnit\Framework\TestCase;

//Add "use" statements for classes of the generated API
use Integration\Test\Client\ApiClient;
use Integration\Test\Client\Models\Error;

class SampleTest extends TestCase {
 
    public function testServiceError() {
        $authProvider = new AnonymousAuthenticationProvider();
        $requestAdapter = new GuzzleRequestAdapter($authProvider);
        $requestAdapter->setBaseUrl('http://127.0.0.1:1080');
        $client = new ApiClient($requestAdapter);
    
        //This api call will return an error response which causes an exception.
        try {
           $client->api()->v1()->topics()->get()->wait();
           $this->fail("No exception was thrown");
        }
        catch(Error $error) {
            $this->assertEquals("my-sample-id", $error->getId());
            $this->assertEquals(123, $error->escapedGetCode());
        }
    }
}
?>