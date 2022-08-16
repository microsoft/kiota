from typing import List

from kiota.abstractions.request_option import RequestOption


class ParametersNameDecodingHandlerOption(RequestOption):
    """The ParametersNameDecodingOptions request class
    """

    parameters_name_decoding_handler_options_key = "ParametersNameDecodingOptionKey"

    def __init__(
        self, enable: bool = True, characters_to_decode: List[str] = [".", "-", "~", "$"]
    ) -> None:
        """To create an instance of ParametersNameDecodingHandlerOptions

        Args:
            enable (bool, optional): - Whether to decode the specified characters in the
            request query parameters names.
            Defaults to True.
            characters_to_decode (List[str], optional):- The characters to decode.
            Defaults to [".", "-", "~", "$"].
        """
        self.enable = enable
        self.characters_to_decode = characters_to_decode

    def get_key(self) -> str:
        return self.parameters_name_decoding_handler_options_key
