-------------------------------------------------------------------------------------------------------------------------------------

- Hi! Thank you for purchasing my Asset!

-ADS---------------------------------------------------------------------------------------------------------------------------------

- In order to work advertising in the application you need
activate Unity Ads in Unity Services!

- Advertising in the game (Scripts/Ads):
RewardedAdsMenu (Additional coins in the menu) and RewardedAdsPlay (Double coins after defeat)

- How to change "Double coins"?
- They are on the stage "Play" -> "Canvas" -> "DoubleCoinMenu"
- You can change the value of coin loss in the script "RewardedAdsPlay"

- If you stop advertising, turn it off, restart Unity and turn it on, or look for a response to Google!

-IN-APP-PURCHASING-------------------------------------------------------------------------------------------------------------------

- In order to work purchases in the application you need
activate Unity In-App Purchasing in Unity Services!

- In-game purchases (Scripts/IAP):
IAPManager (Product_Name = "IDInGooglePlay" is the ID of your product on Google Play)

- Setting up in-game purchases:
https://www.youtube.com/watch?v=EZ_Du9JedPM - RUS
https://www.youtube.com/watch?v=3IQ-CvBQz0o - ENG

-GOOGLE-PLAY-SERVICES----------------------------------------------------------------------------------------------------------------

- For Google Play Services to download and install them in Unity,
Before installing the project with the game should be open, follow the link:
https://github.com/playgameservices/play-games-plugin-for-unity

- Install Google Play Services:
https://www.youtube.com/watch?v=kPy3_SBekNk - RUS
https://www.youtube.com/watch?v=XmHXl-UFTqM - ENG

- Setting Achievements and Leaderboards and so on:
https://www.youtube.com/watch?v=hQHhT8QWbEA - RUS
https://www.youtube.com/watch?v=BhSJK-Kn8Uw - ENG

-LANGUAGE----------------------------------------------------------------------------------------------------------------------------

- How to change or add languages ​​in the game?

We go in the folder (Scripts/Language), in the folder there are two scripts "MenuLanguage" (Text in the Main menu) and "PlayLanguage" (Text in the game)
In the script, when you first start the game, you are prompted to choose a language (Language = PlayerPrefs.GetString ("Language", "Start");)
In order to add another language you need to copy two methods of one of the languages ​​(EnglishStart and English)
Then we change your new language:
- Change the name of the language
- Change the text for all parameters
- Change the line if (Language == "Ru") to -> if (Language! = "Eng") (Instead of "Eng" there can be your language)
- Change Language = "Eng"; into your new language

- In "void Start" add your language:
if (Language == "YourLanguage") {
	YourLanguage ();
	YourLanguage.SetActive (true);
	RuChek.SetActive (false);
	EngChek.SetActive (false);
}

- Also do not forget to add a new language button in the "GameSettings", "LanguageSelection" and add a new language to the script "PlayLanguage"!
- To change the language, you just need to rename one of the languages ​​to your own, change the text of the parameters and add a picture of the language!

-SKINS-SHIPS-------------------------------------------------------------------------------------------------------------------------

- How can I change the skins of ships?
- Skins of the ships are "Sprites/Ships"
- Prefabs of the ships are "Prefabs/Enemy" or "Prefabs/Player/Ships"

- Changing the skin of the ships of enemies occurs using prefabs and the script "EnemyGenerator" ("GeneratorMES" on stage)
- Change the skin of the player's ship takes place in the code with the synchronization of the selected ship in the main menu "ShipController"

-MORE-OF-OUR-GAMES-------------------------------------------------------------------------------------------------------------------

- How do I change the "More Our Games" button?
- If you just want to change the link then insert your link "MainCamera" -> "MainMenu" -> "URL" (void GooglePlayPage)

-------------------------------------------------------------------------------------------------------------------------------------

Sincerely,
Reqoobo Games





