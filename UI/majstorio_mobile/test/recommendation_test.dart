import 'package:flutter_test/flutter_test.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/screens/home_screen.dart';

import 'screens_test.dart' show app, signInAs;
import 'test_data.dart';

Map<String, dynamic> tradespersonJson({List<String> reasons = const [], int userId = 20}) => {
  'id': 3,
  'userId': userId,
  'fullName': 'Emir Kovač',
  'cityId': 1,
  'cityName': 'Mostar',
  'description': 'Vodoinstalater',
  'isApproved': true,
  'isActive': true,
  'averageRating': 4.8,
  'reviewCount': 5,
  'completedJobs': 7,
  'categories': ['Vodoinstalacije'],
  'recommendationReasons': reasons,
};

void main() {
  setUp(() {
    AuthSession.clear();
  });

  testWidgets('home shows a recommended tradesperson with the reason', (tester) async {
    signInAs(AuthSession.customer);
    var api = FakeApi((request) {
      var isProfiles = request.url.path.endsWith('TradespersonProfiles');
      return jsonResponse({
        'items': isProfiles
            ? [
                tradespersonJson(reasons: ['Category', 'City', 'Rating']),
              ]
            : [],
        'totalCount': isProfiles ? 1 : 0,
      });
    });

    await tester.pumpWidget(app(const HomeScreen()));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.text('Emir Kovač'), 200);

    expect(find.text('Preporučeno za vas'), findsOneWidget);
    expect(
      find.text('Preporuka: kategorija koju tražite, grad koji tražite, visoka ocjena 4.8'),
      findsOneWidget,
    );
    var call = api.calls.firstWhere((request) => request.url.path.endsWith('TradespersonProfiles'));
    // No filters, so the API does not record this as a search.
    expect(call.url.queryParameters.keys, unorderedEquals(['page', 'pageSize']));
  });

  testWidgets('home does not recommend the user to themselves', (tester) async {
    signInAs(AuthSession.customer, userId: 20);
    FakeApi((request) {
      var isProfiles = request.url.path.endsWith('TradespersonProfiles');
      return jsonResponse({
        'items': isProfiles ? [tradespersonJson()] : [],
        'totalCount': isProfiles ? 1 : 0,
      });
    });

    await tester.pumpWidget(app(const HomeScreen()));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.text('Trenutno nema preporučenih majstora.'), 200);

    expect(find.text('Emir Kovač'), findsNothing);
  });
}
