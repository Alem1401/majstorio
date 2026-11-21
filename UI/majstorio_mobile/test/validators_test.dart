import 'package:flutter_test/flutter_test.dart';
import 'package:majstorio_mobile/utils/validators.dart' as validate;

void main() {
  test('money must be positive with at most two decimals and within the limit', () {
    expect(validate.money(''), isNotNull);
    expect(validate.money('0'), isNotNull);
    expect(validate.money('-5'), isNotNull);
    expect(validate.money('10.555'), isNotNull);
    expect(validate.money('abc'), isNotNull);
    expect(validate.money('10.5'), isNull);
    expect(validate.money('10,55'), isNull);
    expect(validate.money('120', max: 100), isNotNull);
    expect(validate.money('100', max: 100), isNull);
    expect(validate.parseMoney('10,55'), 10.55);
  });

  test('e-mail and phone formats', () {
    expect(validate.email(''), isNotNull);
    expect(validate.email('ime@'), isNotNull);
    expect(validate.email('ime@primjer.ba'), isNull);
    expect(validate.phone(''), isNull);
    expect(validate.phone('abc'), isNotNull);
    expect(validate.phone('+387 61 123 456'), isNull);
  });

  test('password is required', () {
    expect(validate.password(''), isNotNull);
    expect(validate.password('abc'), isNotNull);
    expect(validate.password('test'), isNull);
  });
}
