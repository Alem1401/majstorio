import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/service_request/service_request_search_request.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/providers/offer/offer_provider.dart';
import 'package:majstorio_mobile/providers/service_request/service_request_provider.dart';
import 'package:majstorio_mobile/utils/error_message.dart';

import 'test_data.dart';

// A token whose middle part holds {"sub":"5","unique_name":"mobile"}.
String fakeToken() {
  var payload = base64Url.encode(utf8.encode(json.encode({'sub': '5', 'unique_name': 'mobile'}))).replaceAll('=', '');
  return 'header.$payload.signature';
}

void main() {
  // The 401 handling uses the app's navigator key, which needs the Flutter binding.
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    AuthSession.clear();
  });

  test('a list sends the filters and the token, and reads the page', () async {
    AuthSession.token = 'abc';
    var api = FakeApi((request) => jsonResponse(page([requestJson(id: 1), requestJson(id: 2)])));

    var result = await ServiceRequestProvider().get(ServiceRequestSearchRequest(status: 0, page: 2, pageSize: 5));

    expect(result.items.map((request) => request.id), [1, 2]);
    expect(api.calls.single.url.path, '/ServiceRequests');
    expect(api.calls.single.url.queryParameters, {'page': '2', 'pageSize': '5', 'status': '0'});
    expect(api.calls.single.headers['Authorization'], 'Bearer abc');
  });

  test('a command with an empty 200 answer succeeds and notifies the screens', () async {
    var api = FakeApi((request) => http.Response('', 200));
    var provider = OfferProvider();
    var notified = 0;
    provider.addListener(() => notified++);

    await provider.accept(3);

    expect(api.calls.single.method, 'POST');
    expect(api.calls.single.url.path, '/Offers/3/accept');
    expect(notified, 1);
  });

  test('validation errors come back per field', () async {
    FakeApi((request) => jsonResponse({
      'errors': {'Price': ['Price must be greater than 0.']},
    }, 400));

    expect(
      () => OfferProvider().withdraw(1),
      throwsA(isA<ApiException>()
          .having((e) => e.statusCode, 'status', 400)
          .having((e) => e.fieldErrors['price'], 'price', 'Price must be greater than 0.')),
    );
  });

  test('a business error shows the server message', () async {
    FakeApi((request) => jsonResponse({'message': 'The offer is no longer pending.'}, 409));

    expect(
      () => OfferProvider().accept(1),
      throwsA(isA<ApiException>()
          .having((e) => e.statusCode, 'status', 409)
          .having((e) => e.message, 'message', 'The offer is no longer pending.')),
    );
  });

  test('a 403 without a body gets a neutral message', () async {
    FakeApi((request) => http.Response('', 403));

    expect(
      () => ServiceRequestProvider().getById(1),
      throwsA(isA<ApiException>().having((e) => e.message, 'message', 'Nemate dozvolu za ovu radnju.')),
    );
  });

  test('a lost connection on a change asks to check before repeating', () async {
    FakeApi((request) => throw const SocketException('offline'));

    expect(
      () => ServiceRequestProvider().cancel(1, null),
      throwsA(isA<ApiException>().having((e) => e.message, 'message', contains('Osvježite podatke'))),
    );
  });

  test('a 401 clears the session of the user who made the call', () async {
    AuthSession.token = 'abc';
    FakeApi((request) => http.Response('', 401));

    await expectLater(() => ServiceRequestProvider().getById(1), throwsA(isA<ApiException>()));
    expect(AuthSession.isLoggedIn, isFalse);
  });

  test('login keeps the role and the user id from the token', () async {
    FakeApi((request) => jsonResponse({
      'accessToken': fakeToken(),
      'expiresAtUtc': '2030-01-01T00:00:00',
      'role': 'Customer',
    }));

    var message = await AuthProvider().login('mobile', 'test');

    expect(message, isNull);
    expect(AuthSession.isCustomer, isTrue);
    expect(AuthSession.userId, 5);
    expect(AuthSession.userName, 'mobile');
  });

  test('an administrator is sent to the desktop app and not signed in', () async {
    FakeApi((request) => jsonResponse({
      'accessToken': fakeToken(),
      'expiresAtUtc': '2030-01-01T00:00:00',
      'role': 'Administrator',
    }));

    var message = await AuthProvider().login('desktop', 'test');

    expect(message, contains('desktop'));
    expect(AuthSession.isLoggedIn, isFalse);
  });

  test('wrong credentials stay on the form with a message', () async {
    FakeApi((request) => http.Response('', 401));

    var message = await AuthProvider().login('mobile', 'wrong');

    expect(message, 'Pogrešno korisničko ime ili lozinka.');
    expect(AuthSession.isLoggedIn, isFalse);
  });
}
