import pytest
from kiota_abstractions.authentication.anonymous_authentication_provider import (
    AnonymousAuthenticationProvider,
)
from kiota_http.httpx_request_adapter import HttpxRequestAdapter

from client.api_client import ApiClient
from client.models.weather_forecast import WeatherForecast
from client.models.weather_forecast_enum_value import WeatherForecast_enumValue

import datetime
from uuid import UUID

@pytest.mark.asyncio
async def test_defaultvalues():
    auth_provider = AnonymousAuthenticationProvider()
    request_adapter = HttpxRequestAdapter(auth_provider)
    request_adapter.base_url = 'http://127.0.0.1:1080'
    client = ApiClient(request_adapter)

    #Call a sample endpoint - not really needed here.
    serviceResponse = await client.api.v1.weather_forecast.get()
    assert serviceResponse != None
    assert len(serviceResponse) == 1

    #Now the real test: create a model class and verify that all properties have the default values.
    model = WeatherForecast()

    assert model.bool_value == True

    assert model.date_only_value != None
    assert model.date_only_value.isoformat() == '1900-01-01'

    assert model.date_value != None
    assert model.date_value.isoformat() == '1900-01-01T00:00:00+00:00'

    assert model.decimal_value == 25.5
    assert model.double_value == 25.5
    assert model.enum_value ==  WeatherForecast_enumValue("one")
    assert model.float_value == 25.5

    assert model.guid_value != None;
    assert str(model.guid_value) == '00000000-0000-0000-0000-000000000000'

    assert model.long_value == 255
    assert model.summary == 'Test'
    assert model.temperature_c == 15

    assert model.time_value != None
    assert model.time_value.isoformat() == '00:00:00'

