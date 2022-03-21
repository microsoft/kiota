export function validateProtocol(url: string): void {
    if(!url.toLocaleLowerCase().startsWith('https://') && !windowUrlStartsWithHttps()) {
        throw new Error('Authentication scheme can only be used with https requests');
    }
}
function windowUrlStartsWithHttps(): boolean {
    // @ts-ignore
    return window && window.location && (window.location.protocol as string).toLowerCase() !== 'https:';
}
