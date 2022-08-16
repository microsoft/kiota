from typing import Dict
from urllib.parse import unquote

from kiota.abstractions.request_option import RequestOption

from requests import PreparedRequest, Response

from .middleware import BaseMiddleware
from .options import ParametersNameDecodingHandlerOption


class ParametersNameDecodingHandler(BaseMiddleware):

    def __init__(
        self,
        options: ParametersNameDecodingHandlerOption = ParametersNameDecodingHandlerOption(),
        **kwargs
    ):
        """Create an instance of ParametersNameDecodingHandler

        Args:
            options (ParametersNameDecodingHandlerOption, optional): The parameters name
            decoding handler options value.
            Defaults to ParametersNameDecodingHandlerOption
        """
        if not options:
            raise Exception("The options parameter is required.")

        self.options = options

    def send(self, request: PreparedRequest, **kwargs) -> Response:  #type: ignore
        """To execute the current middleware

        Args:
            request (PreparedRequest): The prepared request object
            request_options (Dict[str, RequestOption]): The request options

        Returns:
            Response: The response object.
        """
        current_options = self.options
        options_key = (
            ParametersNameDecodingHandlerOption.parameters_name_decoding_handler_options_key
        )
        request_options = kwargs['request_options']
        if request_options and options_key in request_options:
            current_options = request_options[options_key]

        updated_url: str = request.url  #type: ignore
        if (
            current_options and current_options.enable and '%' in updated_url
            and current_options.characters_to_decode
        ):
            updated_url = unquote(updated_url)
        request.url = updated_url
        response = super().send(request, **kwargs)
        return response
