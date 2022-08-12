from abc import ABC, abstractmethod
from typing import Any, Callable, List, Optional, Tuple, TypeVar

T = TypeVar("T")


class BackingStore(ABC):
    """Stores model information in a different location than the object properties.
    Implementations can provide dirty tracking capabilities, caching capabilities
    or integration with 3rd party stores
    """

    @abstractmethod
    def get(self, key: str) -> Optional[T]:
        """Gets a value from the backing store based on its key. Returns null if the value hasn't
        changed and "ReturnOnlyChangedValues" is true.

        Args:
            key (str): The key to lookup the backing store with.

        Returns:
            Optional[T]: The value from the backing store.
        """
        pass

    @abstractmethod
    def set(self, key: str, value: T) -> None:
        """Sets or updates the stored value for the given key. Will trigger subscriptions callbacks.

        Args:
            key (str): The key to store and retrieve the information.
            value (T): The value to be stored.
        """
        pass

    @abstractmethod
    def enumerate_(self) -> List[Tuple[str, Any]]:
        """Enumerates all the values stored in the backing store. Values will be filtered if
        "ReturnOnlyChangedValues" is true.

        Returns:
            List[Tuple[str, Any]]: The values available in the backing store.
        """
        pass

    @abstractmethod
    def enumerate_keys_for_values_changed_to_null(self) -> List[str]:
        """Enumerates the keys for all values that changed to null.

        Returns:
            List[str]: The keys for the values that changed to null.
        """
        pass

    @abstractmethod
    def subscribe(
        self, callback: Callable[[str, Any, Any], None], subscription_id: Optional[str]
    ) -> str:
        """Creates a subscription to any data change happening.

        Args:
            callback (Callable[[str, Any, Any], None]):Callback to be invoked on data changes
            where the first parameter is the data key, the second the previous value and the third
            the new value.
            subscription_id (Optional[str]): The subscription Id to use.

        Returns:
            str: The subscription Id to use when removing the subscription
        """
        pass

    @abstractmethod
    def unsubscribe(self, subscription_id: str) -> None:
        """Removes a subscription from the store based on its subscription id.

        Args:
            subscription_id (str): The Id of the subscription to remove.
        """
        pass

    @abstractmethod
    def clear(self) -> None:
        """Clears the data stored in the backing store. Doesn't trigger any subscription.
        """
        pass

    @abstractmethod
    def get_is_initialization_completed(self) -> bool:
        """Whether the initialization of the object and/or the initial deserialization has been
        completed to track whether objects have changed.

        Returns:
            bool:
        """
        pass

    @abstractmethod
    def set_is_initialization_completed(self, bool) -> None:
        """Sets whether the initialization of the object and/or the initial deserialization has been
        completed to track whether objects have changed.
        """
        pass

    @abstractmethod
    def get_return_only_changed_values(self) -> bool:
        """Whether to return only values that have changed since the initialization of the object
        when calling the Get and Enumerate methods.

        Returns:
            bool:
        """
        pass

    @abstractmethod
    def set_return_only_changed_values(self, bool) -> None:
        """Sets whether to return only values that have changed since the initialization of the
        object when calling the Get and Enumerate methods.

        Returns:
            bool:
        """
        pass
