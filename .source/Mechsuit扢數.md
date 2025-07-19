-UI
  -建筑用面板
    -染色面板
    -背包
    -自动装配方案

  -装备状态的面板
    -武器冷却，状态
    -背包

-Pawn渲染
    - Core 在Root下挂载WGApparel
    WGApparel下挂载WGApparelHead、WGApparelBody
    模块使用apparelprop的parentTag drawdata等正常绘制，
    使用renderprop挂载到WGAp上来渲染额外需要渲染的东西（或者在comp里挂）

  -隐藏原版的躯体
  -自动炮塔的渲染(射击点偏移)
  -光效

-装备
  -核心和子部件的关系(拓展性?)
  -结构，护甲，护盾

-建筑
  -整备坞
  -部件架

-杂项
  -战地维修
