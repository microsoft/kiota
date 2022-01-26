from typing import Any, Callable, Dict, List, Optional, Tuple, TypeVar
from uuid import uuid4

from .backing_store import BackingStore

StoreEntryWrapper = Tuple[bool, Any]
SubscriptionCallback = Callable[[str, Any, Any], None]
StoreEntry = Tuple[str, Any]

T = TypeVar("T")


class InMemoryBackingStore(BackingStore):
    """In-memory implementation of the backing store. Allows for dirty tracking of changes.
    """

    __subscriptions: Dict[str, SubscriptionCallback] = {}
    __store: Dict[str, Tuple[bool, Any]] = {}
    __initialization_completed: bool = True

    return_only_changed_values: bool = False

    def get(self, key: str) -> Optional[T]:
        """Gets the specified object with the given key from the store.

        Args:
            key (str): The key to search with

        Returns:
            Optional[T]: An instance of T
        """
        wrapper = self.__store.get(key)

        if wrapper and (
            self.return_only_changed_values and wrapper[0] or not self.return_only_changed_values
        ):
            return wrapper[1]
        return None

    def set(self, key: str, value: T) -> None:
        """Sets the specified object with the given key in the store.

        Args:
            key (str): The key to use
            value (T): The object value to store
        """
        old_value_wrapper = self.__store.get(key)
        old_value = None
        if old_value_wrapper:
            old_value = old_value_wrapper[1]
        self.__store[key] = (self.get_is_initialization_completed(), value)
        for val in self.__subscriptions.values():
            val(key, old_value, value)

    def enumerate_(self) -> List[StoreEntry]:
        """Enumerate the values in the store based on the ReturnOnlyChangedValues configuration
        value

        Returns:
            List[StoreEntry]: A collection of changed values or the whole store based on the
            ReturnOnlyChangedValues configuration value.
        """
        filterable_array = list(self.__store.items())
        if self.return_only_changed_values:
            filtered = [elem for elem in filterable_array if elem[0] is True]
            return filtered
        return filterable_array

    def enumerate_keys_for_values_changed_to_null(self) -> List[str]:
        """Enumerate the values in the store that have changed to None

        Returns:
            List[str]: A collection of strings containing keys changed to None
        """
        keys: List[str] = []
        for key, val in self.__store.items():
            if val[0] and not val[1]:
                keys.append(key)
        return keys

    def subscribe(
        self, callback: Callable[[str, Any, Any], None], subscription_id: Optional[str]
    ) -> str:
        """dds a callback to subscribe to events in the store with the given subscription id

        Args:
            callback (Callable[[str, Any, Any], None]): The callback to add
            subscription_id (Optional[str]): The subscription id to use for subscription

        Returns:
            str: The id of the subscription
        """
        if not subscription_id:
            subscription_id = str(uuid4())
        self.__subscriptions[subscription_id] = callback
        return subscription_id

    def unsubscribe(self, subscription_id: str) -> None:
        """De-register the callback with the given subscriptionId

        Args:
            subscription_id (str): The id of the subscription to de-register
        """
        del self.__subscriptions[subscription_id]

    def clear(self) -> None:
        """Clears the store
        """
        self.__store.clear()

    def get_is_initialization_completed(self) -> bool:
        """Flag to show the initialization status of the store.
        """
        return self.__initialization_completed

    def set_is_initialization_completed(self, value: bool) -> None:
        self.__initialization_completed = value
        for key, val in self.__store.items():
            self.__store[key] = (not value, val[1])

    def get_return_only_changed_values(self) -> bool:
        """Determines whether the backing store should only return changed values when queried.
        """
        return self.return_only_changed_values

    def set_return_only_changed_values(self, value: bool) -> None:
        """Sets the flag to determines whether the backing store should only return changed values
        when queried.
        """
        self.return_only_changed_values = value
