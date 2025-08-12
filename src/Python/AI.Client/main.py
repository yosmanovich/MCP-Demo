import asyncio
from AIClient import AIClient

async def main():
    client = AIClient()
    try:
        await client.MessageLoop()
    finally:
        await client.cleanup()

if __name__ == "__main__":
    asyncio.run(main())