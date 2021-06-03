export function toFirstCharacterUpper(source?: string): string {
    if(source && source.length > 0) {
        return source.substring(0, 1).toLocaleUpperCase() + source.substring(1);
    } else {
        return '';
    }
}