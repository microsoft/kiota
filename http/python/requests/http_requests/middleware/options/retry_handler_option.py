from typing import Set

from kiota.abstractions.request_option import RequestOption

class RetryHandlerOptions(RequestOption):
    """The retry request option class
    """
    
    DEFAULT_MAX_RETRIES: int = 3
    MAX_RETRIES: int = 10
    DEFAULT_DELAY: int = 3
    MAX_DELAY: int = 180
    DEFAULT_BACKOFF_FACTOR: float = 0.5
    MAXIMUM_BACKOFF: int = 120
    _DEFAULT_RETRY_STATUS_CODES: Set[int] = {429, 503, 504}
    
    def __init__(self) -> None:
        super().__init__()
        self._max_retry: int = self.DEFAULT_MAX_RETRIES
        self._retry_backoff_factor: float = self.DEFAULT_BACKOFF_FACTOR
        self._retry_backoff_max: int = self.MAXIMUM_BACKOFF
        self._retry_time_limit: float = self.MAX_DELAY
        self._retry_on_status_codes: Set[int] = self._DEFAULT_RETRY_STATUS_CODES
        self._allowed_methods: Set[str] = frozenset(
            ['HEAD', 'GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS']
        )
        self._respect_retry_after_header: bool = True
    
    @property
    def max_retry(self) -> int:
        return self._max_retry
    
    @max_retry.setter
    def max_retry(self, value: int) -> None:
        if value > self.MAX_RETRIES:
            raise ValueError("Maximum value for max retry property exceeded.")
        self._max_retry = value
        
    @property
    def backoff_factor(self) -> float:
        return self._retry_backoff_factor
    
    @backoff_factor.setter
    def backoff_factor(self, value: float) -> None:
        self._retry_backoff_factor = value
        
    @property
    def backoff_max(self) -> int:
        return self._retry_backoff_max
    
    @backoff_max.setter
    def backoff_max(self, value: int) -> None:
        self._retry_backoff_max = value
        
    @property
    def retry_time_limit(self) -> int:
        return self._retry_time_limit
    
    @retry_time_limit.setter
    def retry_time_limit(self, value: int) -> None:
        if value > self.MAX_DELAY:
            raise ValueError("Maximum value for retry time limit property exceeded.")
        self._retry_time_limit = value
        
    @property
    def retry_on_status_codes(self) -> Set[int]:
        return self._retry_on_status_codes
    
    @property
    def allowed_methods(self) -> Set[str]:
        return self._allowed_methods
    
    @property
    def respect_retry_after_header(self) -> bool:
        return self._respect_retry_after_header
    
    @classmethod
    def disable_retries(cls):
        """
        Disable retries by setting retry_total to zero.
        retry_total takes precedence over all other counts.
        """
        cls.max_retry = 0
        return cls
    
    def get_key():
        pass