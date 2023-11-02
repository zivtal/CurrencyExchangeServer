# Currency exchange server
## Client to get exchange currency rate by Bank Of Israel

When converting between two foreign currencies (that is, currencies other than the Israeli shekel), a two-step conversion is performed.

1- First, the source currency is converted to the Israeli shekel.

2- After that, the Israeli shekel is converted to the target foreign currency.

The result gives the conversion rate between the two foreign currencies.

In addition, the service provides a new array that includes conversion rates between every currency available in the Bank of Israel. The data manipulation process includes:

1- Each currency is first converted to the Israeli Shekel.

2- Each currency is then converted to any other listed foreign currency.

In fact, this service creates a comprehensive matrix of conversion rates between all the currencies provided by the Bank of Israel using the Israeli Shekel as an intermediary in the conversion process.

API:
/Currency/Exchange?from=USD&to=ILS&amount=100
/Currency/Rates
