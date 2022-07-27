from dataclasses import dataclass
from typing import Dict
@dataclass
class DummyToken:
    token: str
        
class DummyAzureTokenCredential():
    
    async def get_token(*args):
        return DummyToken(token="This is a dummy token")
    
    async def close(*args):
        pass
    