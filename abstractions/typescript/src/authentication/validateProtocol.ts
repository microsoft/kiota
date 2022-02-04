export function validateProtocol(url: string): void {
  // @ts-ignore
  if (
    !url.toLocaleLowerCase().startsWith("https://") ||
    (window && (window.location.protocol as string).toLowerCase() !== "https:")
  ) {
    throw new Error(
      "Authentication scheme can only be used with https requests"
    );
  }
}
