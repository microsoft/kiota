package com.microsoft.kiota;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class QueryParametersBaseTest {

    @Test
    void SetsSelectQueryParameters() {
        var requestInfo = new RequestInformation()
        {{
            this.httpMethod = HttpMethod.GET;
            this.urlTemplate = "http://localhost/me{?%24select}";
        }};
        var qParams = new GetQueryParameters() {{
            this.Select = new String[] { "id", "displayName" };
        }};

        // Act
        qParams.AddQueryParameters(requestInfo.queryParameters);

        // Assert
        assertTrue(requestInfo.queryParameters.containsKey("%24select"));
        assertFalse(requestInfo.queryParameters.containsKey("select"));
    }
    
}
class GetQueryParameters extends QueryParametersBase
{
    @QueryParameter(name = "%24select")
    public String[] Select;
    @QueryParameter(name = "%24count")
    public Boolean Count;
    @QueryParameter(name = "%24filter")
    public String Filter;
    @QueryParameter(name = "%24orderby")
    public String[] Orderby;
    @QueryParameter(name = "%24search")
    public String Search;
}
