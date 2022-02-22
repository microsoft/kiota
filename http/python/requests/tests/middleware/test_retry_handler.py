from email.utils import formatdate
from time import time

import pytest
import requests
import responses

from http_requests.middleware.options import RetryHandlerOptions
from http_requests.middleware import RetryHandler

BASE_URL = 'https://httpbin.org'

def test_no_config():
    """
    Test that default values are used if no custom confguration is passed
    """
    options = RetryHandlerOptions()
    retry_handler = RetryHandler()
    assert retry_handler.max_retries == options.DEFAULT_MAX_RETRIES
    assert retry_handler.timeout == options.MAX_DELAY
    assert retry_handler.backoff_max == options.MAXIMUM_BACKOFF
    assert retry_handler.backoff_factor == options.DEFAULT_BACKOFF_FACTOR
    assert retry_handler.allowed_methods == frozenset(
        ['HEAD', 'GET', 'PUT', 'POST', 'PATCH', 'DELETE', 'OPTIONS']
    )
    assert retry_handler.respect_retry_after_header
    assert retry_handler.retry_on_status_codes == options._DEFAULT_RETRY_STATUS_CODES


def test_custom_options():
    """
    Test that default configuration is overrriden if custom configuration is provided
    """
    options = RetryHandlerOptions()
    options.max_retry = 1
    options._retry_backoff_factor = 0.2
    options.retry_time_limit = 100
    
    retry_handler = RetryHandler(options)

    assert retry_handler.max_retries == 1
    assert retry_handler.timeout == 100
    assert retry_handler.backoff_factor == 0.2


@responses.activate
def test_method_retryable_with_valid_method():
    """
    Test if method is retryable with a retryable request method.
    """
    responses.add(responses.GET, BASE_URL, status=502)
    response = requests.get(BASE_URL)

    retry_handler = RetryHandler(RetryHandlerOptions())
    assert retry_handler._is_method_retryable(response.request)


@responses.activate
def test_should_retry_valid():
    """
    Test the should_retry method with a valid HTTP method and response code
    """
    responses.add(responses.GET, BASE_URL, status=503)
    response = requests.get(BASE_URL)

    options = RetryHandlerOptions()
    retry_handler = RetryHandler(RetryHandlerOptions())
    assert retry_handler.should_retry(response)


@responses.activate
def test_should_retry_invalid():
    """
    Test the should_retry method with an invalid HTTP response code
    """
    responses.add(responses.GET, BASE_URL, status=502)
    response = requests.get(BASE_URL)

    retry_handler = RetryHandler(RetryHandlerOptions())

    assert not retry_handler.should_retry(response)


@responses.activate
def test_is_request_payload_buffered_valid():
    """
    Test for _is_request_payload_buffered helper method.
    Should return true request payload is buffered/rewindable.
    """
    responses.add(responses.GET, BASE_URL, status=429)
    response = requests.get(BASE_URL)

    retry_handler = RetryHandler(RetryHandlerOptions())

    assert retry_handler._is_request_payload_buffered(response)


@responses.activate
def test_is_request_payload_buffered_invalid():
    """
    Test for _is_request_payload_buffered helper method.
    Should return false if request payload is forward streamed.
    """
    responses.add(responses.POST, BASE_URL, status=429)
    response = requests.post(BASE_URL, headers={'Content-Type': "application/octet-stream"})

    retry_handler = RetryHandler(RetryHandlerOptions())

    assert not retry_handler._is_request_payload_buffered(response)


def test_check_retry_valid():
    """
    Test that a retry is valid if the maximum number of retries has not been reached
    """
    retry_handler = RetryHandler(RetryHandlerOptions())

    assert retry_handler.check_retry_valid(0)


def test_check_retry_valid_no_retries():
    """
    Test that a retry is not valid if maximum number of retries has been reached
    """
    options = RetryHandlerOptions()
    options.max_retry = 2
    retry_handler = RetryHandler(options)

    assert not retry_handler.check_retry_valid(2)


@responses.activate
def test_get_retry_after():
    """
    Test the _get_retry_after method with an integer value for retry header.
    """
    responses.add(responses.GET, BASE_URL, headers={'Retry-After': "120"}, status=503)
    response = requests.get(BASE_URL)

    retry_handler = RetryHandler(RetryHandlerOptions())

    assert retry_handler._get_retry_after(response) == 120


@responses.activate
def test_get_retry_after_no_header():
    """
    Test the _get_retry_after method with no Retry-After header.
    """
    responses.add(responses.GET, BASE_URL, status=503)
    response = requests.get(BASE_URL)

    retry_handler = RetryHandler(RetryHandlerOptions())

    assert retry_handler._get_retry_after(response) is None


@responses.activate
def test_get_retry_after_http_date():
    """
    Test the _get_retry_after method with a http date as Retry-After value.
    """
    timevalue = time() + 120
    http_date = formatdate(timeval=timevalue, localtime=False, usegmt=True)
    responses.add(responses.GET, BASE_URL, headers={'retry-after': f'{http_date}'}, status=503)
    response = requests.get(BASE_URL)

    retry_handler = RetryHandler(RetryHandlerOptions())
    assert retry_handler._get_retry_after(response) < 120
    
def test_disable_retries():
    """
    Test that when disable_retries class method is called, total retries are set to zero
    """
    options = RetryHandlerOptions.disable_retries()
    
    retry_handler = RetryHandler(options)
    assert retry_handler.max_retries == 0
    assert not retry_handler.check_retry_valid(0)