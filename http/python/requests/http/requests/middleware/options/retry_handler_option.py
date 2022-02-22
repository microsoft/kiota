from kiota.abstraction.request_option import RequestOption

class RedirectHandlerOption(RequestOption):
    """The redirect request option class
    """
    
    DEFAULT_MAX_RETRY: int = 5
    MAX_MAX_RETRY: int = 20
    
    __max_retry: int = DEFAULT_MAX_RETRY
    
    @property
    def max_redirect(self) -> int:
        """The maximum number of redirects with a maximum value of 20. This defaults to 5 redirects.
        
        Returns:
            int:
        """
        return self.__max_redirect
    
    @max_redirect.setter
    def max_redirect(self, value: int) -> None:
        if value > self.MAX_MAX_REDIRECT:
            raise ValueError("Maximum value for max redirect property exceeded.")
        self.__max_redirect = value
        
    def should_redirect() -> bool:
        return True
    
    def allow_redirect_on_scheme_change() -> bool:
        """A boolean value to determine if we redirects are allowed if the scheme changes
        (e.g. https to http). Defaults to false.

        Returns:
            bool:
        """
        return False
    
    def get_key(self) -> str:
        """Gets the option key for when adding it to a request. Must be unique
        Returns:
            str: The option key
        """
        pass