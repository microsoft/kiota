import imp
from typing import List
from urllib import request
from abc import ABC
from typing import List, Optional
from kiota.abstractions.request_options import RequestOption

class Middleware(ABC):
    """Defines the contract for a middleware in the request execution pipeline.
    """
    # Next middleware to be executed. The current middleware must execute it in its implementation.
    next: Optional[Middleware]
    
    def execute(url: str, req: RequestInit, request_options: List[Optional[RequestOption]]) -> Response:
        """Main method of the middleware.

        Args:
            url (str):The URL of the request.
            req (request):The request object.
            request_options: (List[Optional[RequestOption]]): _description_

        Returns:
            Response: A promise that resolves to the response object.
        """