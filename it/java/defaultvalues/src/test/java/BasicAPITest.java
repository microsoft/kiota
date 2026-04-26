import apisdk.ApiClient;
import apisdk.models.WeatherForecast;
import apisdk.models.WeatherForecastEnumValue;
import com.microsoft.kiota.authentication.AnonymousAuthenticationProvider;
import com.microsoft.kiota.http.OkHttpRequestAdapter;
import static org.junit.jupiter.api.Assertions.*;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.util.concurrent.TimeUnit;
import java.util.List;

public class BasicAPITest {

    @Test
    void basicTest() throws Exception {
        var adapter = new OkHttpRequestAdapter(new AnonymousAuthenticationProvider());
        adapter.setBaseUrl("http://127.0.0.1:1080");
        var client = new ApiClient(adapter);

        //Call a sample endpoint - not really needed here.
        List<WeatherForecast> modelList = client.api().v1().weatherForecast().get();
        assertNotNull(modelList);
        assertEquals(1, modelList.size());

        //Now the real test: create a model class and verify that all properties have the default values.
        WeatherForecast model = new WeatherForecast();
        assertTrue(model.getBoolValue());

        assertNotNull(model.getDateOnlyValue());
        assertEquals("1900-01-01", model.getDateOnlyValue().toString());

        assertNotNull(model.getDateValue());
        //No seconds are printed, and the timezone is "Z".
        assertEquals("1900-01-01T00:00Z", model.getDateValue().toString());

        assertEquals(25.5, model.getDecimalValue());
        assertEquals(25.5, model.getDoubleValue());
        assertEquals(WeatherForecastEnumValue.One, model.getEnumValue());
        assertEquals(25.5f, model.getFloatValue());

        assertNotNull(model.getGuidValue());
        assertEquals("00000000-0000-0000-0000-000000000000", model.getGuidValue().toString());

        assertEquals(255, model.getLongValue());
        assertEquals("Test", model.getSummary());
        assertEquals(15, model.getTemperatureC());

        assertNotNull(model.getTimeValue());
        //Seconds are not printed if 0.
        assertEquals("00:00", model.getTimeValue().toString());
    }

}
