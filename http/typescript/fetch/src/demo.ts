import { HttpClient } from "./httpClient"



async function name(): Promise<void> {
    const httpClient = new HttpClient(undefined, null);
    const context = {
        request:"url",
        options:{method:"GET"}
    }

    const s = await httpClient.executeFetch(context);


}

name().then().catch((err)=> err);



