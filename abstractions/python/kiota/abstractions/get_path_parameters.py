from typing import Any, Dict, Optional, Union

from .request_information import RequestInformation


def get_path_parameters(parameters: Union[Dict[str, Any], Optional[str]]) -> Dict[str, Any]:
    result: Dict[str, Any] = {}
    if isinstance(parameters, str):
        result[RequestInformation.RAW_URL_KEY] = parameters
    elif isinstance(parameters, dict):
        for key, val in parameters.items():
            result[key] = val
    return result
