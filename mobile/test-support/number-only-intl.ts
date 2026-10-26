// Node accepts BigInt here, while the affected Android Hermes Intl bridge rejects it.
// Keep that device constraint in every render test; this is not a Hermes emulator.
export function installNumberOnlyIntl() {
  const prototype = Intl.NumberFormat.prototype;
  const format = Object.getOwnPropertyDescriptor(prototype, 'format')!;
  Object.defineProperty(prototype, 'format', {
    ...format,
    get(this: Intl.NumberFormat) {
      const original = format.get!.call(this) as (value: number | bigint) => string;
      return (value: number | bigint) => {
        if (typeof value === 'bigint') throw new TypeError('Cannot convert bigint to number');
        return original(value);
      };
    },
  });
  const parts = prototype.formatToParts;
  prototype.formatToParts = function (value: number | bigint) {
    if (typeof value === 'bigint') throw new TypeError('Cannot convert bigint to number');
    return parts.call(this, value);
  };
}
